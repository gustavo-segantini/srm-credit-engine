using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.DTOs.Responses;
using SrmCreditEngine.Application.Interfaces;
using SrmCreditEngine.Infrastructure.Data;

namespace SrmCreditEngine.Infrastructure.Analytics;

/// <summary>
/// Dapper-based analytics query for settlement statements.
/// Bypasses EF Core for performance on large datasets.
/// Uses parameterized SQL to prevent SQL injection.
/// </summary>
public sealed class SettlementStatementQuery : ISettlementStatementQuery
{
    private readonly AppDbContext _dbContext;

    public SettlementStatementQuery(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SettlementStatementResponse> GetStatementAsync(
        GetSettlementStatementRequest request,
        CancellationToken cancellationToken = default)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        var conditions = new List<string>();

        if (request.From.HasValue)
        {
            conditions.Add("s.created_at >= @From");
            parameters.Add("From", request.From.Value.ToUniversalTime());
        }

        if (request.To.HasValue)
        {
            conditions.Add("s.created_at <= @To");
            parameters.Add("To", request.To.Value.ToUniversalTime());
        }

        if (request.CedentId.HasValue)
        {
            conditions.Add("r.cedent_id = @CedentId");
            parameters.Add("CedentId", request.CedentId.Value);
        }

        if (request.PaymentCurrency.HasValue)
        {
            conditions.Add("s.payment_currency = @PaymentCurrency");
            parameters.Add("PaymentCurrency", (int)request.PaymentCurrency.Value);
        }

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        var offset = (request.Page - 1) * request.PageSize;
        parameters.Add("Limit", request.PageSize);
        parameters.Add("Offset", offset);

        var sql = $"""
            SELECT
                s.id              AS SettlementId,
                r.id              AS ReceivableId,
                r.document_number AS DocumentNumber,
                c.name            AS CedentName,
                c.cnpj            AS CedentCnpj,
                r.type            AS ReceivableTypeInt,
                s.face_value      AS FaceValue,
                s.face_currency   AS FaceCurrencyInt,
                s.net_disbursement AS NetDisbursement,
                s.payment_currency AS PaymentCurrencyInt,
                s.discount        AS Discount,
                CASE WHEN s.face_value > 0
                     THEN ROUND((s.discount / s.face_value) * 100, 4)
                     ELSE 0
                END               AS DiscountRatePercent,
                s.status          AS StatusInt,
                s.settled_at      AS SettledAt,
                s.created_at      AS CreatedAt
            FROM credit.settlements s
            INNER JOIN credit.receivables r ON r.id = s.receivable_id
            INNER JOIN credit.cedents c ON c.id = r.cedent_id
            {whereClause}
            ORDER BY s.created_at DESC
            LIMIT @Limit OFFSET @Offset
            """;

        var countSql = $"""
            SELECT COUNT(*)
            FROM credit.settlements s
            INNER JOIN credit.receivables r ON r.id = s.receivable_id
            INNER JOIN credit.cedents c ON c.id = r.cedent_id
            {whereClause}
            """;

        var aggregateSql = $"""
            SELECT
                COALESCE(SUM(s.face_value), 0)       AS TotalFaceValue,
                COALESCE(SUM(s.net_disbursement), 0) AS TotalNetDisbursement,
                COALESCE(SUM(s.discount), 0)         AS TotalDiscount
            FROM credit.settlements s
            INNER JOIN credit.receivables r ON r.id = s.receivable_id
            INNER JOIN credit.cedents c ON c.id = r.cedent_id
            {whereClause}
            """;

        var rawItems = await connection.QueryAsync<SettlementStatementRaw>(sql, parameters);
        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var aggregates = await connection.QuerySingleAsync<SettlementAggregates>(aggregateSql, parameters);

        var items = rawItems.Select(row => new SettlementStatementItemResponse(
            SettlementId: row.SettlementId,
            ReceivableId: row.ReceivableId,
            DocumentNumber: row.DocumentNumber,
            CedentName: row.CedentName,
            CedentCnpj: row.CedentCnpj,
            ReceivableType: ((Domain.Enums.ReceivableType)row.ReceivableTypeInt).ToString(),
            FaceValue: row.FaceValue,
            FaceCurrency: ((Domain.Enums.CurrencyCode)row.FaceCurrencyInt).ToString(),
            NetDisbursement: row.NetDisbursement,
            PaymentCurrency: ((Domain.Enums.CurrencyCode)row.PaymentCurrencyInt).ToString(),
            Discount: row.Discount,
            DiscountRatePercent: row.DiscountRatePercent,
            Status: ((Domain.Enums.SettlementStatus)row.StatusInt).ToString(),
            SettledAt: row.SettledAt,
            CreatedAt: row.CreatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new SettlementStatementResponse(
            Items: items,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: totalPages,
            TotalFaceValue: aggregates.TotalFaceValue,
            TotalNetDisbursement: aggregates.TotalNetDisbursement,
            TotalDiscount: aggregates.TotalDiscount
        );
    }

    // Private query result types (not part of domain)
    private sealed record SettlementStatementRaw(
        Guid SettlementId, Guid ReceivableId, string DocumentNumber, string CedentName,
        string CedentCnpj, int ReceivableTypeInt, decimal FaceValue, int FaceCurrencyInt,
        decimal NetDisbursement, int PaymentCurrencyInt, decimal Discount,
        decimal DiscountRatePercent, int StatusInt, DateTime? SettledAt, DateTime CreatedAt);

    private sealed record SettlementAggregates(
        decimal TotalFaceValue, decimal TotalNetDisbursement, decimal TotalDiscount);
}
