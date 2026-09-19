Create these files outside the repository with mode `0600`:

- `postgres_password`: PostgreSQL password.
- `ConnectionStrings__Default`: complete runtime PostgreSQL connection string.
- `Jwt__Key`: random JWT signing key containing at least 32 bytes.

The double underscore in a filename maps to a configuration section when the
ASP.NET Core Key-per-file provider reads `/run/secrets`.
