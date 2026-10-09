Run the role/right and direct-page-access checks with:

    dotnet run --project tests/WorkspaceAccess.Checks/WorkspaceAccess.Checks.csproj

These checks compile the production access catalogue, page filter and class assignment handler directly. They cover cross-role access with broad rights, missing rights, combined roles, shared modules, unauthenticated requests, and server-side denial of direct URLs and assignment POSTs. They also validate the assignment model's unique index, foreign key deletion restrictions, and migration SQL preserving existing teacher links. SQL Server metadata and SQL generation are tested without connecting to a database. Live Microsoft sign-in and successful database writes require a configured development environment.
