-- The 15 tables a pool database holds after a typical Compze test on MySQL, as SHOW CREATE TABLE gave them on
-- 2026-11-27: the fixture the measurement tools create and drop as Compze's tests do. {database} is replaced.
CREATE TABLE `{database}`.`databaseprocesslockholder` (
  `LockName` varchar(200) NOT NULL,
  `Holder` varchar(1000) NOT NULL,
  PRIMARY KEY (`LockName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`endpointcatalog` (
  `EndpointName` varchar(64) NOT NULL,
  `EndpointId` char(36) NOT NULL,
  `CreatedUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`EndpointName`),
  UNIQUE KEY `IX_EndpointCatalog_Unique_EndpointId` (`EndpointId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`store` (
  `Id` varchar(500) NOT NULL,
  `ValueTypeId` int NOT NULL,
  `Created` datetime NOT NULL,
  `Updated` datetime NOT NULL,
  `Value` mediumtext NOT NULL,
  PRIMARY KEY (`Id`,`ValueTypeId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`taggregatelock` (
  `TaggregateId` char(36) NOT NULL,
  PRIMARY KEY (`TaggregateId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`testingcontainer_inboxdeliverystreamadmissions` (
  `SenderEndpointId` char(36) NOT NULL,
  `LastAdmittedSequenceNumber` bigint NOT NULL,
  PRIMARY KEY (`SenderEndpointId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`testingcontainer_inboxtessages` (
  `GeneratedId` bigint NOT NULL AUTO_INCREMENT,
  `TypeId` int NOT NULL,
  `TessageId` char(36) NOT NULL,
  `SenderEndpointId` char(36) NOT NULL,
  `DeliveryStreamSequenceNumber` bigint NOT NULL,
  `Status` smallint NOT NULL,
  `Body` mediumtext NOT NULL,
  `ExceptionCount` int NOT NULL DEFAULT '0',
  `ExceptionType` varchar(500) DEFAULT NULL,
  `ExceptionStackTrace` mediumtext,
  `ExceptionTessage` mediumtext,
  `FailingTimeMilliseconds` bigint NOT NULL DEFAULT '0',
  PRIMARY KEY (`GeneratedId`),
  UNIQUE KEY `IX_TestingContainer_InboxTessages_Unique_TessageId` (`TessageId`),
  UNIQUE KEY `IX_TestingContainer_InboxStreamPosition` (`SenderEndpointId`,`DeliveryStreamSequenceNumber`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`testingcontainer_outboxdeliverystreamcounters` (
  `EndpointId` char(36) NOT NULL,
  `LastAssignedSequenceNumber` bigint NOT NULL,
  PRIMARY KEY (`EndpointId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`testingcontainer_outboxtessagedispatching` (
  `TessageId` char(36) NOT NULL,
  `EndpointId` char(36) NOT NULL,
  `DeliveryStreamSequenceNumber` bigint NOT NULL,
  `IsReceived` bit(1) NOT NULL,
  `IsStranded` bit(1) NOT NULL DEFAULT b'0',
  `RetryCount` int NOT NULL DEFAULT '0',
  `LastAttemptTime` datetime DEFAULT NULL,
  `FailureReason` mediumtext,
  PRIMARY KEY (`TessageId`,`EndpointId`),
  UNIQUE KEY `IX_TestingContainer_OutboxStreamPosition` (`EndpointId`,`DeliveryStreamSequenceNumber`),
  CONSTRAINT `testingcontainer_outboxtessagedispatching_ibfk_1` FOREIGN KEY (`TessageId`) REFERENCES `testingcontainer_outboxtessages` (`TessageId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`testingcontainer_outboxtessages` (
  `GeneratedId` bigint NOT NULL AUTO_INCREMENT,
  `TypeId` int NOT NULL,
  `TessageId` char(36) NOT NULL,
  `SerializedTessage` mediumtext NOT NULL,
  PRIMARY KEY (`GeneratedId`),
  UNIQUE KEY `IX_TestingContainer_OutboxTessages_Unique_TessageId` (`TessageId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`testingcontainer_peerhandledtessagetypes` (
  `EndpointId` char(36) NOT NULL,
  `HandledTessageType` mediumtext NOT NULL,
  KEY `EndpointId` (`EndpointId`),
  CONSTRAINT `testingcontainer_peerhandledtessagetypes_ibfk_1` FOREIGN KEY (`EndpointId`) REFERENCES `testingcontainer_peers` (`EndpointId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`testingcontainer_peers` (
  `EndpointId` char(36) NOT NULL,
  PRIMARY KEY (`EndpointId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`tevent` (
  `InsertionOrder` bigint NOT NULL AUTO_INCREMENT,
  `TaggregateId` char(36) NOT NULL,
  `UtcTimeStamp` datetime(6) NOT NULL,
  `TeventType` int NOT NULL,
  `Tevent` mediumtext NOT NULL,
  `TeventId` char(36) NOT NULL,
  `InsertedVersion` int NOT NULL,
  `SqlInsertTimeStamp` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ReadOrder` decimal(38,19) NOT NULL,
  `EffectiveVersion` int NOT NULL,
  `TargetTevent` char(36) DEFAULT NULL,
  `RefactoringType` tinyint DEFAULT NULL,
  PRIMARY KEY (`TaggregateId`,`InsertedVersion`),
  UNIQUE KEY `IX_Tevent_Unique_TeventId` (`TeventId`),
  UNIQUE KEY `IX_Tevent_Unique_InsertionOrder` (`InsertionOrder`),
  UNIQUE KEY `IX_Tevent_Unique_ReadOrder` (`ReadOrder`),
  KEY `TargetTevent` (`TargetTevent`),
  KEY `IX_Tevent_ReadOrder` (`ReadOrder`,`EffectiveVersion`),
  CONSTRAINT `tevent_ibfk_1` FOREIGN KEY (`TargetTevent`) REFERENCES `tevent` (`TeventId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`typeids` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `CurrentName` mediumtext NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`typenames` (
  `TypeId` int NOT NULL,
  `Seq` int NOT NULL,
  `FullyQualifiedName` mediumtext NOT NULL,
  `FirstSeenUtc` datetime NOT NULL,
  PRIMARY KEY (`TypeId`,`Seq`),
  CONSTRAINT `FK_TypeNames_TypeIds` FOREIGN KEY (`TypeId`) REFERENCES `typeids` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
CREATE TABLE `{database}`.`typestrings` (
  `TypeString` mediumtext NOT NULL,
  `TypeId` int NOT NULL,
  `FirstSeenUtc` datetime NOT NULL,
  KEY `IX_TypeStrings_TypeId` (`TypeId`),
  CONSTRAINT `FK_TypeStrings_TypeIds` FOREIGN KEY (`TypeId`) REFERENCES `typeids` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci
;
