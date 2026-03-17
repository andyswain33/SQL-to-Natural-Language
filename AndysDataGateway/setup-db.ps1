Write-Host "--- Initializing Secure Gateway Database ---" -ForegroundColor Cyan

# 1. Copy the Schema
docker cp SampleSchema.sql sql-gateway-dev:/SampleSchema.sql

# 2. Run the SQL Command
Write-Host "Executing Schema Script..."
docker exec -i sql-gateway-dev /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Portfolio-Data-Gatew4y" -C -i /SampleSchema.sql

# 3. Verify
Write-Host "Verifying Data..." -ForegroundColor Yellow
docker exec -i sql-gateway-dev /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Portfolio-Data-Gatew4y" -C -Q "SELECT * FROM [SecureGatewayIAM].[dbo].[vw_ActiveIdentities]"

Write-Host "--- Setup Complete! ---" -ForegroundColor Green