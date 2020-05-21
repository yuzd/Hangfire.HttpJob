-- ----------------------------
-- Table structure for `Job`
-- ----------------------------
DROP TABLE IF EXISTS `hangfire_Job`;
CREATE TABLE `hangfire_Job` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `StateId` int(11) DEFAULT NULL,
  `StateName` varchar(20) DEFAULT NULL,
  `InvocationData` longtext NOT NULL,
  `Arguments` longtext NOT NULL,
  `CreatedAt` datetime NOT NULL,
  `ExpireAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Job_StateName` (`StateName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


-- ----------------------------
-- Table structure for `Counter`
-- ----------------------------
DROP TABLE IF EXISTS `hangfire_Counter`;
CREATE TABLE `hangfire_Counter` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) NOT NULL,
  `Value` int(11) NOT NULL,
  `ExpireAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Counter_Key` (`Key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


DROP TABLE IF EXISTS `hangfire_AggregatedCounter`;
CREATE TABLE `hangfire_AggregatedCounter` (
	Id int(11) NOT NULL AUTO_INCREMENT,
	`Key` varchar(100) NOT NULL,
	`Value` int(11) NOT NULL,
	ExpireAt datetime DEFAULT NULL,
	PRIMARY KEY (`Id`),
	UNIQUE KEY `IX_CounterAggregated_Key` (`Key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


-- ----------------------------
-- Table structure for `DistributedLock`
-- ----------------------------
DROP TABLE IF EXISTS `hangfire_DistributedLock`;
CREATE TABLE `hangfire_DistributedLock` (
  `Resource` varchar(100) NOT NULL,
  `CreatedAt` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


-- ----------------------------
-- Table structure for `Hash`
-- ----------------------------
DROP TABLE IF EXISTS `hangfire_Hash`;
CREATE TABLE `hangfire_Hash` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) NOT NULL,
  `Field` varchar(40) NOT NULL,
  `Value` longtext,
  `ExpireAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Hash_Key_Field` (`Key`,`Field`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


-- ----------------------------
-- Table structure for `JobParameter`
-- ----------------------------
DROP TABLE IF EXISTS `hangfire_JobParameter`;
CREATE TABLE `hangfire_JobParameter` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `JobId` int(11) NOT NULL,
  `Name` varchar(40) NOT NULL,
  `Value` longtext,

  PRIMARY KEY (`Id`),
  CONSTRAINT `IX_JobParameter_JobId_Name` UNIQUE (`JobId`,`Name`),
  KEY `FK_JobParameter_Job` (`JobId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------
-- Table structure for `JobQueue`
-- ----------------------------
DROP TABLE IF EXISTS `hangfire_JobQueue`;
CREATE TABLE `hangfire_JobQueue` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `JobId` int(11) NOT NULL,
  `Queue` varchar(50) NOT NULL,
  `FetchedAt` datetime DEFAULT NULL,
  `FetchToken` varchar(36) DEFAULT NULL,
  
  PRIMARY KEY (`Id`),
  INDEX `IX_JobQueue_QueueAndFetchedAt` (`Queue`,`FetchedAt`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------
-- Table structure for `JobState`
-- ----------------------------
DROP TABLE IF EXISTS `hangfire_JobState`;
CREATE TABLE `hangfire_JobState` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `JobId` int(11) NOT NULL,
  `Name` varchar(20) NOT NULL,
  `Reason` varchar(100) DEFAULT NULL,
  `CreatedAt` datetime NOT NULL,
  `Data` longtext,
  PRIMARY KEY (`Id`),
  KEY `FK_JobState_Job` (`JobId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------
-- Table structure for `Server`
-- ----------------------------
DROP TABLE IF EXISTS `hangfire_Server`;
CREATE TABLE `hangfire_Server` (
  `Id` varchar(100) NOT NULL,
  `Data` longtext NOT NULL,
  `LastHeartbeat` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
 
 
-- ----------------------------
-- Table structure for `Set`
-- ----------------------------
DROP TABLE IF EXISTS `hangfire_Set`;
CREATE TABLE `hangfire_Set` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) NOT NULL,
  `Value` varchar(255) NOT NULL,
  `Score` float NOT NULL,
  `ExpireAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Set_Key_Value` (`Key`,`Value`)
) ENGINE=InnoDB  CHARSET=utf8;


-- DROP TABLE `hangfire_State`;
DROP TABLE IF EXISTS `hangfire_State`;
CREATE TABLE `hangfire_State`
(
	Id int(11) NOT NULL AUTO_INCREMENT,
	JobId int(11) NOT NULL,
	Name varchar(20) NOT NULL,
	Reason varchar(100) NULL,
	CreatedAt datetime NOT NULL,
	Data longtext NULL,
	PRIMARY KEY (`Id`),
	KEY `FK_HangFire_State_Job` (`JobId`)
) ENGINE=InnoDB  CHARSET=utf8;

-- 
DROP TABLE IF EXISTS `hangfire_List`;
CREATE TABLE `hangfire_List`
(
	`Id` int(11) NOT NULL AUTO_INCREMENT,
	`Key` varchar(100) NOT NULL,
	`Value` longtext NULL,
	`ExpireAt` datetime NULL,
	PRIMARY KEY (`Id`)
) ENGINE=InnoDB  CHARSET=utf8mb4;
