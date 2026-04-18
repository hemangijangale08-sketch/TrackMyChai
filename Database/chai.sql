CREATE DATABASE ChaiTrackingDB
USE ChaiTrackingDB

--USERS TABLE--
CREATE TABLE Users(
      Id INT IDENTITY(1,1) PRIMARY KEY,
      Name NVARCHAR(100) NOT NULL,
      Email NVARCHAR(100),
      Password NVARCHAR(100),
      Department NVARCHAR(100),
      Role NVARCHAR(20) DEFAULT 'User'
      );

--REQUEST TABLE--
CREATE TABLE Requests(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT,
    DrinkType NVARCHAR(50),
    Cups INT,
    Status NVARCHAR(50),
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
    );



--add data to table--
INSERT INTO Users(Name,Email,Password,Department,Role)
  VALUES ('Hemangi','hemangi@gmail.com','1234','IT','Admin');

INSERT INTO Users(Name,Email,Password,Department,Role)
  VALUES ('Devyani','dev@gmail.com','dev123','MCA','User');
--REQUEST
INSERT INTO Requests( UserId,DrinkType,Cups,Status)
   VALUES(1,'Tea',2,'Pending');

   SELECT * FROM Users WHERE Role='Admin';

ALTER TABLE Requests ADD Price INT;

UPDATE Requests
SET Price =
    CASE 
        WHEN DrinkType = 'Tea' THEN Cups * 10
        WHEN DrinkType = 'Coffee' THEN Cups * 20
        ELSE 0
    END;

SELECT DrinkType, Cups, Price FROM Requests

--FETCH RECORDS--
Select * from Users
select * from Requests
