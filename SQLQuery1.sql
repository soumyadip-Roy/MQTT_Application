create database meter_readings;

use meter_readings;

CREATE TABLE MeterReadings (
Id INT IDENTITY(1,1) PRIMARY KEY,
Timestamp DATETIME,
MeterId NVARCHAR(50),
CustomerId NVARCHAR(50),
VoltageReading DECIMAL(10,2),
CurrentReading DECIMAL(10,2)
);

select * from MeterReadings;

truncate table MeterReadings;