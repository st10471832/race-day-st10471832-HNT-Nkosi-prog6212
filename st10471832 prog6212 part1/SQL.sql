IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

-- CREATE DATABASE

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

-- TABLE: Organisers

CREATE TABLE Organisers
(
    OrganiserID     INT IDENTITY(1,1) PRIMARY KEY,
    FullName        VARCHAR(100) NOT NULL,
    Email           VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255) NOT NULL,
    Phone           VARCHAR(20) NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- TABLE: Participants

CREATE TABLE Participants
(
    ParticipantID   INT IDENTITY(1,1) PRIMARY KEY,
    FullName        VARCHAR(100) NOT NULL,
    Email           VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255) NOT NULL,
    Phone           VARCHAR(20) NULL,
    DateOfBirth     DATE NOT NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- TABLE: Events

CREATE TABLE Events
(
    EventID         INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID     INT NOT NULL,
    EventName       VARCHAR(150) NOT NULL,
    EventDate       DATE NOT NULL,
    Location        VARCHAR(150) NOT NULL,
    Description     VARCHAR(500) NULL,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Events_Organisers
        FOREIGN KEY (OrganiserID)
        REFERENCES Organisers(OrganiserID)
);
GO


-- TABLE: Categories

CREATE TABLE Categories
(
    CategoryID      INT IDENTITY(1,1) PRIMARY KEY,
    EventID         INT NOT NULL,
    CategoryName    VARCHAR(100) NOT NULL,
    DistanceKm      DECIMAL(5,2) NOT NULL,
    MaxParticipants INT NOT NULL DEFAULT 100,
    Price           DECIMAL(8,2) NOT NULL DEFAULT 0,

    CONSTRAINT FK_Categories_Events
        FOREIGN KEY (EventID)
        REFERENCES Events(EventID)
);
GO


-- TABLE: Enrolments

CREATE TABLE Enrolments
(
    EnrolmentID     INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID      INT NOT NULL,
    ParticipantID   INT NOT NULL,
    EnrolmentDate   DATETIME NOT NULL DEFAULT GETDATE(),
    Status          VARCHAR(20) NOT NULL DEFAULT 'Confirmed',

    CONSTRAINT FK_Enrolments_Categories
        FOREIGN KEY (CategoryID)
        REFERENCES Categories(CategoryID),

    CONSTRAINT FK_Enrolments_Participants
        FOREIGN KEY (ParticipantID)
        REFERENCES Participants(ParticipantID),

    CONSTRAINT UQ_Enrolments_Participant_Category
        UNIQUE (CategoryID, ParticipantID)
);
GO

-- TABLE: Results

CREATE TABLE Results
(
    ResultID        INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID     INT NOT NULL UNIQUE,
    FinishTime      TIME NULL,
    Position        INT NULL,
    Status          VARCHAR(20) NOT NULL DEFAULT 'Finished',

    CONSTRAINT FK_Results_Enrolments
        FOREIGN KEY (EnrolmentID)
        REFERENCES Enrolments(EnrolmentID)
);
GO

-- SEED DATA

-- 2 Organisers

INSERT INTO Organisers
    (FullName, Email, PasswordHash, Phone)
VALUES
    ('Thabo Nkosi',
     'thabo.nkosi@raceday.co.za',
     'hashed_pw_1',
     '0821234567'),

    ('Sarah van Wyk',
     'sarah.vanwyk@raceday.co.za',
     'hashed_pw_2',
     '0837654321');
GO

-- 2 Participants

INSERT INTO Participants
    (FullName, Email, PasswordHash, Phone, DateOfBirth)
VALUES
    ('Lindiwe Dube',
     'lindiwe.dube@example.com',
     'hashed_pw_3',
     '0791112222',
     '1996-04-12'),

    ('James Botha',
     'james.botha@example.com',
     'hashed_pw_4',
     '0824445555',
     '1990-09-23');
GO

-- 3 Events

INSERT INTO Events
    (OrganiserID, EventName, EventDate, Location, Description)
VALUES
    (1,
     'Benoni City Marathon',
     '2026-10-18',
     'Benoni, Gauteng',
     'Annual road race through Benoni CBD and surrounds.'),

    (1,
     'Ekurhuleni Trail Run',
     '2026-11-08',
     'Ekurhuleni, Gauteng',
     'Off-road trail event across three distances.'),

    (2,
     'Cape Coastal Fun Run',
     '2026-12-05',
     'Cape Town, Western Cape',
     'Family-friendly coastal run with a kids fun run.');
GO

-- Categories for each event

INSERT INTO Categories
    (EventID, CategoryName, DistanceKm, MaxParticipants, Price)
VALUES
    (1, '10km',        10.0, 500, 150.00),
    (1, '21km',        21.1, 300, 250.00),
    (2, '5km Trail',    5.0, 200, 120.00),
    (2, '15km Trail',  15.0, 150, 200.00),
    (3, '5km Fun Run',   5.0, 400, 100.00);
GO

-- Sample enrolments

INSERT INTO Enrolments
    (CategoryID, ParticipantID, Status)
VALUES
    (1, 1, 'Confirmed'),
    (2, 2, 'Confirmed'),
    (3, 1, 'Confirmed'),
    (5, 2, 'Confirmed');
GO

-- Sample results

INSERT INTO Results
    (EnrolmentID, FinishTime, Position, Status)
VALUES
    (1, '00:52:14', 3, 'Finished'),
    (2, '01:48:30', 12, 'Finished');
GO
