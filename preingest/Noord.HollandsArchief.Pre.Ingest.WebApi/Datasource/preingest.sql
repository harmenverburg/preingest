--
-- File generated with SQLiteStudio v3.2.1 on Tue Dec 15 23:13:26 2020
--
-- Text encoding used: System
--
PRAGMA foreign_keys = off;
--BEGIN TRANSACTION;

-- Table: Messages
DROP TABLE IF EXISTS Messages;

CREATE TABLE Messages (
    MessageId   VARCHAR (36) NOT NULL
                             CONSTRAINT PK_Messages PRIMARY KEY,
    StateId     VARCHAR (36) NOT NULL,
    Creation    DATETIME     NOT NULL,
    Description TEXT,
    CONSTRAINT FK_Messages_States_StateId FOREIGN KEY (
        StateId
    )
    REFERENCES States (StateId) ON DELETE CASCADE
);


-- Table: Sessions
DROP TABLE IF EXISTS Sessions;

CREATE TABLE Sessions (
    ProcessId           VARCHAR (36)    NOT NULL
                                        CONSTRAINT PK_Sessions PRIMARY KEY,
    FolderSessionId     VARCHAR (36)    NOT NULL,
    Name                VARCHAR (255),
    Description         TEXT,
    Creation            DATETIME        NOT NULL,
    ResultFiles         TEXT
);


-- Table: States
DROP TABLE IF EXISTS States;

CREATE TABLE States (
    StateId   VARCHAR (36)  NOT NULL
                            CONSTRAINT PK_States PRIMARY KEY,
    SessionId VARCHAR (36)  NOT NULL,
    Name      VARCHAR (255),
    Creation  DATETIME      NOT NULL,
    CONSTRAINT FK_States_Sessions_SessionId FOREIGN KEY (
        SessionId
    )
    REFERENCES Sessions (SessionId) ON DELETE CASCADE
);


-- Index: IX_Messages_StateId
DROP INDEX IF EXISTS IX_Messages_StateId;

CREATE INDEX IX_Messages_StateId ON Messages (
    "StateId"
);


-- Index: IX_States_SessionId
DROP INDEX IF EXISTS IX_States_SessionId;

CREATE INDEX IX_States_SessionId ON States (
    "SessionId"
);


--COMMIT TRANSACTION;
PRAGMA foreign_keys = on;
