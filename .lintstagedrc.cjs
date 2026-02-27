/**
 * lint-staged configuration for the SRM Credit Engine monorepo.
 *
 * Rules:
 *  - Any staged .ts/.tsx in apps/frontend → full ESLint pass on the frontend
 *    (running the whole project is safer than per-file for TS type-checking)
 *  - Any staged .cs → dotnet format check on the backend
 */

module.exports = {
  // Frontend — TS/TSX files trigger a full ESLint run
  'apps/frontend/src/**/*.{ts,tsx}': () => 'npm --prefix apps/frontend run lint',

  // Backend — C# files trigger dotnet format verification
  'apps/backend/**/*.cs': () =>
    'dotnet format apps/backend/SrmCreditEngine.sln --verify-no-changes --severity warn',
};
