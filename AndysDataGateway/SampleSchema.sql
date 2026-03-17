-- 1. DATABASE SETUP
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SecureGatewayIAM')
BEGIN
    CREATE DATABASE SecureGatewayIAM;
END
GO

USE SecureGatewayIAM;
GO

-- 2. CLEANUP (Ensures fresh start for seed data)
DROP VIEW IF EXISTS vw_HighSecurityAccessLogs;
DROP VIEW IF EXISTS vw_ActiveIdentities;
DROP TABLE IF EXISTS AccessAttempts;
DROP TABLE IF EXISTS Credentials;
DROP TABLE IF EXISTS SecurityZones;
DROP TABLE IF EXISTS Identities;
GO

-- =======================================================================
-- 3. BASE TABLES
-- =======================================================================

CREATE TABLE Identities (
    IdentityID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    JobTitle NVARCHAR(100),
    Department NVARCHAR(100),
    ClearanceLevel INT DEFAULT 1,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
);

CREATE TABLE SecurityZones (
    ZoneID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ZoneName NVARCHAR(100) NOT NULL,
    Building NVARCHAR(100) NOT NULL,
    IsHighSecurity BIT DEFAULT 0,
    RequiredClearanceLevel INT DEFAULT 1,
    CapacityLimit INT NULL
);

CREATE TABLE Credentials (
    CredentialID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    IdentityID UNIQUEIDENTIFIER NOT NULL,
    CredentialType NVARCHAR(50) NOT NULL,
    SerialNumber NVARCHAR(100) UNIQUE,
    IssuedDate DATETIME2 DEFAULT SYSUTCDATETIME(),
    ExpiryDate DATETIME2 NOT NULL,
    IsRevoked BIT DEFAULT 0,
    CONSTRAINT FK_Credentials_Identities FOREIGN KEY (IdentityID) REFERENCES Identities(IdentityID)
);

CREATE TABLE AccessAttempts (
    AttemptID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CredentialID UNIQUEIDENTIFIER NOT NULL,
    ZoneID UNIQUEIDENTIFIER NOT NULL,
    AttemptTimestamp DATETIME2 DEFAULT SYSUTCDATETIME(),
    IsGranted BIT NOT NULL,
    DenialReason NVARCHAR(255) NULL,
    RiskScore DECIMAL(5,2) DEFAULT 0.00,
    CONSTRAINT FK_AccessAttempts_Credentials FOREIGN KEY (CredentialID) REFERENCES Credentials(CredentialID),
    CONSTRAINT FK_AccessAttempts_SecurityZones FOREIGN KEY (ZoneID) REFERENCES SecurityZones(ZoneID)
);
GO

-- =======================================================================
-- 4. ABSTRACTION LAYER
-- =======================================================================

CREATE VIEW vw_ActiveIdentities AS
SELECT 
    IdentityID, FirstName, LastName, Email, Department, JobTitle, ClearanceLevel
FROM Identities
WHERE IsActive = 1;
GO

CREATE VIEW vw_HighSecurityAccessLogs AS
SELECT 
    aa.AttemptTimestamp, z.ZoneName, z.Building,
    i.FirstName + ' ' + i.LastName AS IdentityName,
    c.CredentialType, aa.IsGranted, aa.DenialReason, aa.RiskScore
FROM AccessAttempts aa
INNER JOIN SecurityZones z ON aa.ZoneID = z.ZoneID
INNER JOIN Credentials c ON aa.CredentialID = c.CredentialID
INNER JOIN Identities i ON c.IdentityID = i.IdentityID
WHERE z.IsHighSecurity = 1;
GO

-- =======================================================================
-- 5. SEED DATA
-- =======================================================================

DECLARE @Id1 UNIQUEIDENTIFIER = NEWID(), @Id2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Zone1 UNIQUEIDENTIFIER = NEWID(), @Zone2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Cred1 UNIQUEIDENTIFIER = NEWID(), @Cred2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Identities (IdentityID, FirstName, LastName, Email, JobTitle, Department, ClearanceLevel)
VALUES 
(@Id1, 'Alice', 'Smith', 'alice.smith@enterprise.com', 'Systems Engineer', 'IT', 3),
(@Id2, 'Bob', 'Jones', 'bob.jones@enterprise.com', 'Marketing Specialist', 'Marketing', 1);

INSERT INTO SecurityZones (ZoneID, ZoneName, Building, IsHighSecurity, RequiredClearanceLevel)
VALUES 
(@Zone1, 'Main Lobby', 'HQ-Alpha', 0, 1),
(@Zone2, 'Server Room A', 'HQ-Alpha', 1, 3);

INSERT INTO Credentials (CredentialID, IdentityID, CredentialType, SerialNumber, ExpiryDate)
VALUES 
(@Cred1, @Id1, 'Biometric', 'BIO-001', DATEADD(year, 1, SYSUTCDATETIME())),
(@Cred2, @Id2, 'MobileNFC', 'MOB-882', DATEADD(year, 1, SYSUTCDATETIME()));

INSERT INTO AccessAttempts (CredentialID, ZoneID, AttemptTimestamp, IsGranted, DenialReason, RiskScore)
VALUES 
(@Cred1, @Zone2, DATEADD(hour, -2, SYSUTCDATETIME()), 1, NULL, 5.0),
(@Cred2, @Zone2, DATEADD(hour, -1, SYSUTCDATETIME()), 0, 'Clearance Too Low', 85.5);
GO