CREATE DATABASE  IF NOT EXISTS `new_account_zf` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `new_account_zf`;
-- MySQL dump 10.13  Distrib 8.0.24, for Win64 (x86_64)
--
-- Host: localhost    Database: new_account_zf
-- ------------------------------------------------------
-- Server version	5.7.34-log

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `account`
--

DROP TABLE IF EXISTS `account`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account` (
  `AccountID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `Username` varchar(16) NOT NULL,
  `Password` varchar(70) NOT NULL,
  `Salt` varchar(45) DEFAULT NULL,
  `AuthorityID` smallint(6) unsigned NOT NULL DEFAULT '1',
  `StatusID` smallint(6) unsigned NOT NULL DEFAULT '1',
  `IPAddress` varchar(45) DEFAULT NULL,
  `MacAddress` varchar(64) NOT NULL DEFAULT '',
  `Registered` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ParentId` int(4) NOT NULL DEFAULT '0',
  PRIMARY KEY (`AccountID`) USING BTREE,
  UNIQUE KEY `AccountID_UNIQUE` (`AccountID`) USING BTREE,
  UNIQUE KEY `Username_UNIQUE` (`Username`) USING BTREE,
  KEY `fk_account_account_authority_idx` (`AuthorityID`) USING BTREE,
  KEY `fk_account_ftw_parent_00_idx` (`ParentId`),
  CONSTRAINT `fk_account_account_authority` FOREIGN KEY (`AuthorityID`) REFERENCES `account_authority` (`AuthorityID`),
  CONSTRAINT `fk_account_ftw_parent_00` FOREIGN KEY (`ParentId`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `account_authority`
--

DROP TABLE IF EXISTS `account_authority`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_authority` (
  `AuthorityID` smallint(5) unsigned NOT NULL AUTO_INCREMENT,
  `AuthorityName` varchar(45) NOT NULL,
  PRIMARY KEY (`AuthorityID`) USING BTREE,
  UNIQUE KEY `AuthorityID_UNIQUE` (`AuthorityID`) USING BTREE,
  UNIQUE KEY `AuthorityName_UNIQUE` (`AuthorityName`) USING BTREE,
  KEY `AuthorityID` (`AuthorityID`) USING BTREE,
  KEY `AuthorityID_2` (`AuthorityID`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=256 DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `account_ban`
--

DROP TABLE IF EXISTS `account_ban`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_ban` (
  `Id` int(10) NOT NULL AUTO_INCREMENT,
  `Data` int(10) NOT NULL DEFAULT '0' COMMENT 'Data should be used for secondary Identities. Example: Conquer account ID ban.',
  `AccountId` int(10) NOT NULL DEFAULT '0' COMMENT 'FTW Account Identity.',
  `BanTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ExpireTime` datetime NOT NULL DEFAULT '2199-12-31 23:59:59',
  `BannedBy` int(10) NOT NULL DEFAULT '0',
  `ReasonId` int(11) NOT NULL DEFAULT '0' COMMENT 'Predefined reason for fixed ban settings. Moderators or whoever is doing the ban should not choose the ban time.',
  `Reason` text NOT NULL,
  `Flags` int(11) NOT NULL DEFAULT '0',
  `UpdatedAt` datetime DEFAULT NULL,
  `DeletedAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `fk_account_ban_user_idx` (`AccountId`),
  KEY `fk_account_ban_reason_idx` (`ReasonId`),
  KEY `fk_account_ban_admin_fkx_idx` (`BannedBy`),
  CONSTRAINT `fk_account_ban_admin_fkx` FOREIGN KEY (`BannedBy`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_account_ban_reason` FOREIGN KEY (`ReasonId`) REFERENCES `account_ban_reason` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_account_ban_user` FOREIGN KEY (`AccountId`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=latin1 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `account_ban_reason`
--

DROP TABLE IF EXISTS `account_ban_reason`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_ban_reason` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(64) NOT NULL DEFAULT 'NoReason',
  `Type` int(11) NOT NULL DEFAULT '0' COMMENT '0 for FTW Account\n1 for Game Account',
  `GameType` int(11) DEFAULT '0' COMMENT 'Specify game type if it''s game relative.',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=24 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `account_ban_reason_factor`
--

DROP TABLE IF EXISTS `account_ban_reason_factor`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_ban_reason_factor` (
  `ReasonId` int(11) NOT NULL,
  `Ocurrences` int(11) NOT NULL DEFAULT '0',
  `Minutes` int(11) NOT NULL DEFAULT '1440',
  `Permanent` tinyint(1) NOT NULL DEFAULT '0',
  `AllowCancel` tinyint(1) NOT NULL DEFAULT '0',
  `CanBeForgiven` tinyint(1) NOT NULL DEFAULT '0',
  UNIQUE KEY `UQ_UniqueOcurrence` (`ReasonId`,`Ocurrences`),
  CONSTRAINT `fk_breason_ban_reason_factor` FOREIGN KEY (`ReasonId`) REFERENCES `account_ban_reason` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `account_device_codes`
--

DROP TABLE IF EXISTS `account_device_codes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_device_codes` (
  `UserCode` varchar(256) NOT NULL,
  `DeviceCode` varchar(45) NOT NULL,
  `SubjectId` varchar(45) DEFAULT NULL,
  `SessionId` varchar(45) DEFAULT NULL,
  `ClientId` varchar(45) NOT NULL,
  `Description` varchar(45) DEFAULT NULL,
  `CreationTime` varchar(45) NOT NULL,
  `Expiration` varchar(45) NOT NULL,
  `Data` varchar(21000) NOT NULL,
  PRIMARY KEY (`UserCode`),
  UNIQUE KEY `IX_DeviceCodes_DeviceCode` (`DeviceCode`),
  KEY `IX_DeviceCodes_Expiration` (`Expiration`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `account_logins`
--

DROP TABLE IF EXISTS `account_logins`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_logins` (
  `LoginProvider` varchar(128) NOT NULL,
  `ProviderKey` varchar(128) NOT NULL,
  `ProviderDisplayName` varchar(256) DEFAULT NULL,
  `UserId` int(11) NOT NULL,
  PRIMARY KEY (`LoginProvider`,`ProviderKey`),
  KEY `PK_AspNetUserLogins` (`LoginProvider`,`ProviderKey`),
  KEY `FK_AspNetUserLogins_AspNetUsers_UserId_idx` (`UserId`),
  CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `account_persisted_grants`
--

DROP TABLE IF EXISTS `account_persisted_grants`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_persisted_grants` (
  `Key` varchar(200) NOT NULL,
  `Type` varchar(64) NOT NULL,
  `SubjectId` varchar(256) DEFAULT NULL,
  `SessionId` varchar(128) DEFAULT NULL,
  `ClientId` varchar(256) NOT NULL,
  `Description` varchar(256) DEFAULT NULL,
  `CreationTime` datetime NOT NULL,
  `Expiration` datetime DEFAULT NULL,
  `ConsumedTime` datetime DEFAULT NULL,
  `Data` varchar(21000) NOT NULL,
  PRIMARY KEY (`Key`),
  KEY `IX_PersistedGrants_Expiration` (`Expiration`),
  KEY `IX_PersistedGrants_SubjectId_ClientId_Type` (`SubjectId`,`ClientId`,`Type`),
  KEY `IX_PersistedGrants_SubjectId_SessionId_Type` (`SubjectId`,`SessionId`,`Type`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `account_role_claims`
--

DROP TABLE IF EXISTS `account_role_claims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_role_claims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `RoleId` int(11) NOT NULL DEFAULT '0',
  `ClaimType` varchar(256) DEFAULT NULL,
  `ClaimValue` varchar(256) DEFAULT NULL,
  `CreationDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `FK_AspNetRoleClaims_AspNetRoles_RoleId_idx` (`RoleId`),
  CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `account_roles` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=45 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `account_roles`
--

DROP TABLE IF EXISTS `account_roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_roles` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(256) NOT NULL,
  `NormalizedName` varchar(256) NOT NULL,
  `ConcurrencyStamp` varchar(256) DEFAULT NULL,
  `CreationDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id_UNIQUE` (`Id`),
  UNIQUE KEY `NormalizedName_UNIQUE` (`NormalizedName`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `account_tokens`
--

DROP TABLE IF EXISTS `account_tokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_tokens` (
  `UserId` int(11) NOT NULL,
  `LoginProvider` varchar(128) NOT NULL,
  `Name` varchar(128) NOT NULL,
  `Value` varchar(256) DEFAULT NULL,
  PRIMARY KEY (`UserId`,`LoginProvider`,`Name`),
  CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `account_user_claims`
--

DROP TABLE IF EXISTS `account_user_claims`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_user_claims` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserId` int(11) DEFAULT NULL,
  `ClaimType` varchar(128) DEFAULT NULL,
  `ClaimValue` varchar(256) DEFAULT NULL,
  `CreationDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `FK_AspNetUserClaims_AspNetUsers_UserId_idx` (`UserId`),
  CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `accounts` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=23 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `account_user_roles`
--

DROP TABLE IF EXISTS `account_user_roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_user_roles` (
  `UserId` int(11) NOT NULL,
  `RoleId` int(11) NOT NULL,
  `CreationDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime DEFAULT NULL,
  PRIMARY KEY (`UserId`,`RoleId`),
  KEY `FK_AspNetUserRoles_AspNetRoles_RoleId_idx` (`RoleId`),
  CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `account_roles` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION,
  CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `accounts` (`Id`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COMMENT='	';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `account_vip_bind`
--

DROP TABLE IF EXISTS `account_vip_bind`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_vip_bind` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `UserId` int(11) NOT NULL,
  `ServerId` int(10) unsigned NOT NULL,
  `TargetId` int(11) DEFAULT NULL,
  `CardId` int(11) DEFAULT NULL,
  `BindTime` datetime NOT NULL,
  `ExpirationTime` datetime NOT NULL,
  `CancellationTime` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `FK_BIND_VIP_USER_ID_idx` (`UserId`),
  KEY `FK_BIND_VIP_SERVER_ID_idx` (`ServerId`),
  KEY `FK_BIND_VIP_CARD_ID_idx` (`CardId`),
  CONSTRAINT `FK_BIND_VIP_CARD_ID` FOREIGN KEY (`CardId`) REFERENCES `credit_card` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `FK_BIND_VIP_SERVER_ID` FOREIGN KEY (`ServerId`) REFERENCES `realm` (`RealmID`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `FK_BIND_VIP_USER_ID` FOREIGN KEY (`UserId`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `accounts`
--

DROP TABLE IF EXISTS `accounts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `accounts` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserName` varchar(128) NOT NULL DEFAULT '',
  `NormalizedUserName` varchar(128) NOT NULL,
  `PasswordHash` varchar(256) DEFAULT NULL,
  `Salt` varchar(128) DEFAULT NULL,
  `Type` tinyint(4) DEFAULT '0',
  `Flag` bigint(20) DEFAULT '0',
  `Email` varchar(128) NOT NULL DEFAULT '',
  `NormalizedEmail` varchar(128) NOT NULL,
  `EmailConfirmed` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `PhoneNumber` varchar(20) NOT NULL DEFAULT '',
  `PhoneNumberConfirmed` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `ConcurrencyStamp` varchar(128) DEFAULT NULL,
  `SecurityStamp` varchar(128) DEFAULT NULL,
  `LockoutEnabled` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `LockoutEnd` datetime DEFAULT NULL,
  `AccessFailedCount` int(11) NOT NULL DEFAULT '0',
  `TwoFactorEnabled` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `SecurityCode` bigint(20) unsigned NOT NULL DEFAULT '0',
  `SecurityQuestion` int(11) DEFAULT NULL,
  `SecurityAnswer` varchar(128) NOT NULL DEFAULT '',
  `VipPoints` int(11) NOT NULL DEFAULT '0',
  `Credits` bigint(20) NOT NULL DEFAULT '0',
  `Lock` int(11) DEFAULT NULL,
  `CreationDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Id_UNIQUE` (`Id`),
  UNIQUE KEY `Email_UNIQUE` (`Email`),
  UNIQUE KEY `UserName_UNIQUE` (`UserName`),
  KEY `FK_SecurityQuestion_idx` (`SecurityQuestion`),
  KEY `FK_Account_User_Lock_idx` (`Lock`),
  CONSTRAINT `FK_Account_User_Lock` FOREIGN KEY (`Lock`) REFERENCES `account_ban` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `FK_SecurityQuestion` FOREIGN KEY (`SecurityQuestion`) REFERENCES `security_questions` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COMMENT='This table stores the accounts related to website stuff.';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `article`
--

DROP TABLE IF EXISTS `article`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `article` (
  `Id` int(4) unsigned NOT NULL AUTO_INCREMENT,
  `CategoryId` int(11) NOT NULL DEFAULT '0',
  `UserId` int(11) NOT NULL DEFAULT '0',
  `Flags` bigint(16) unsigned NOT NULL DEFAULT '1',
  `ThumbId` int(10) DEFAULT NULL,
  `Views` int(11) NOT NULL DEFAULT '0',
  `PublishDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `CreationDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  `DeletedDate` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Article_Category_idx` (`CategoryId`),
  KEY `FK_Article_Creator_Identifier_idx` (`UserId`),
  CONSTRAINT `FK_Article_Category` FOREIGN KEY (`CategoryId`) REFERENCES `article_category` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `FK_Article_Creator_Identifier` FOREIGN KEY (`UserId`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `article_category`
--

DROP TABLE IF EXISTS `article_category`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `article_category` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(45) NOT NULL DEFAULT '',
  `CategoryTypeId` int(11) NOT NULL DEFAULT '1',
  `UserId` int(11) NOT NULL DEFAULT '0',
  `Flags` int(11) NOT NULL DEFAULT '0',
  `CreationDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime DEFAULT NULL,
  `DeletedDate` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `FK_Article_Category_Type_idx` (`CategoryTypeId`),
  CONSTRAINT `FK_Article_Category_Type` FOREIGN KEY (`CategoryTypeId`) REFERENCES `article_category_type` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `article_category_type`
--

DROP TABLE IF EXISTS `article_category_type`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `article_category_type` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(45) NOT NULL DEFAULT '',
  `Flags` int(11) NOT NULL DEFAULT '0',
  `UserId` int(11) NOT NULL DEFAULT '0',
  `CreationDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime DEFAULT NULL,
  `DeletedDate` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `article_comment`
--

DROP TABLE IF EXISTS `article_comment`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `article_comment` (
  `Id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `ArticleId` int(11) unsigned NOT NULL DEFAULT '0',
  `UserId` int(11) NOT NULL DEFAULT '0',
  `TokenId` varchar(128) NOT NULL DEFAULT '',
  `IpAddress` varchar(15) NOT NULL DEFAULT '0.0.0.0',
  `Message` text NOT NULL,
  `Flag` int(10) NOT NULL DEFAULT '0',
  `Likes` int(11) NOT NULL DEFAULT '0',
  `Dislikes` int(11) NOT NULL DEFAULT '0',
  `CreationDate` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdateDate` timestamp NULL DEFAULT NULL,
  `DeleteDate` timestamp NULL DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Identity_UNIQUE` (`Id`),
  KEY `FK_Comment_Article_ID_0_idx` (`ArticleId`),
  KEY `FK_Comment_User_Author_ID_0_idx` (`UserId`),
  CONSTRAINT `FK_Comment_Article_ID_0` FOREIGN KEY (`ArticleId`) REFERENCES `article` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `FK_Comment_User_Author_ID_0` FOREIGN KEY (`UserId`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8 COMMENT='Stores the comments done in an article or guide.';
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `article_comment_reaction`
--

DROP TABLE IF EXISTS `article_comment_reaction`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `article_comment_reaction` (
  `Id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `CommentId` int(10) unsigned NOT NULL DEFAULT '0',
  `UserId` int(10) unsigned NOT NULL DEFAULT '0',
  `Reaction` int(10) unsigned NOT NULL DEFAULT '0',
  `Rating` int(10) unsigned NOT NULL DEFAULT '0',
  `Timestamp` int(11) DEFAULT '0',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Identity_UNIQUE` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `article_content`
--

DROP TABLE IF EXISTS `article_content`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `article_content` (
  `Id` int(4) unsigned NOT NULL AUTO_INCREMENT,
  `ArticleId` int(4) unsigned NOT NULL,
  `WriterId` int(11) NOT NULL DEFAULT '0',
  `Locale` varchar(8) NOT NULL,
  `Title` varchar(255) NOT NULL,
  `Content` longtext NOT NULL,
  `LastEditorId` int(11) DEFAULT '0',
  `CreationDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  `DeletedDate` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_Article_Identifier_idx` (`ArticleId`),
  KEY `FK_Article_Initial_Creator_idx` (`WriterId`),
  KEY `FK_Article_Last_Editor_idx` (`LastEditorId`),
  CONSTRAINT `FK_Article_Identifier` FOREIGN KEY (`ArticleId`) REFERENCES `article` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `FK_Article_Initial_Creator` FOREIGN KEY (`WriterId`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `FK_Article_Last_Editor` FOREIGN KEY (`LastEditorId`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `article_reaction`
--

DROP TABLE IF EXISTS `article_reaction`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `article_reaction` (
  `Id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `ArticleId` int(10) unsigned NOT NULL DEFAULT '0',
  `UserId` int(10) unsigned NOT NULL DEFAULT '0',
  `Reaction` int(10) unsigned NOT NULL DEFAULT '0',
  `Rating` int(10) unsigned NOT NULL DEFAULT '0',
  `Timestamp` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Identity_UNIQUE` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `article_read_token`
--

DROP TABLE IF EXISTS `article_read_token`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `article_read_token` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ArticleId` int(11) unsigned NOT NULL,
  `Referer` varchar(256) NOT NULL DEFAULT '/',
  `TokenId` varchar(128) NOT NULL,
  `UserId` int(11) DEFAULT NULL,
  `IpAddress` varchar(64) NOT NULL,
  `UserAgent` varchar(256) NOT NULL DEFAULT '',
  `CreationDate` int(11) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `FK_User_Read_Token_0_idx` (`UserId`),
  KEY `FK_Article_Read_Token_0_idx` (`ArticleId`),
  CONSTRAINT `FK_Article_Read_Token_0` FOREIGN KEY (`ArticleId`) REFERENCES `article` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `FK_User_Read_Token_0` FOREIGN KEY (`UserId`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `credit_card`
--

DROP TABLE IF EXISTS `credit_card`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `credit_card` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Type` int(11) NOT NULL DEFAULT '1',
  `UserId` int(11) DEFAULT NULL,
  `CheckoutId` bigint(20) DEFAULT NULL,
  `Part1` smallint(4) unsigned zerofill NOT NULL DEFAULT '0000',
  `Part2` smallint(4) unsigned zerofill NOT NULL DEFAULT '0000',
  `Part3` smallint(4) unsigned zerofill NOT NULL DEFAULT '0000',
  `Part4` smallint(4) unsigned zerofill NOT NULL DEFAULT '0000',
  `Password` varchar(16) NOT NULL DEFAULT '',
  `Credi` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UsedAt` datetime DEFAULT NULL,
  `CanceledAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `UQ_CARD_ID` (`Part1`,`Part2`,`Part3`,`Part4`,`Password`),
  KEY `FK_CC_USER_OWNER_ID_idx` (`UserId`),
  CONSTRAINT `FK_CC_USER_OWNER_ID` FOREIGN KEY (`UserId`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `discord_channel`
--

DROP TABLE IF EXISTS `discord_channel`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `discord_channel` (
  `Id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `ChannelId` bigint(20) unsigned NOT NULL DEFAULT '0',
  `Name` varchar(255) NOT NULL,
  `CreatedAt` int(11) NOT NULL DEFAULT '0',
  `Default` tinyint(1) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `UQ_IDX_DC_CHANNEL` (`ChannelId`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=latin1 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `discord_message`
--

DROP TABLE IF EXISTS `discord_message`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `discord_message` (
  `Id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `UserId` bigint(20) unsigned NOT NULL DEFAULT '0',
  `CurrentUserName` varchar(255) NOT NULL,
  `ChannelId` bigint(20) unsigned NOT NULL DEFAULT '0',
  `Message` text NOT NULL,
  `Timestamp` int(11) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`) USING BTREE,
  KEY `FK_DC_MSG_DC_USER_idx` (`UserId`),
  KEY `FK_CHANNEL_ID_MESSAGE_idx` (`ChannelId`),
  CONSTRAINT `FK_CHANNEL_ID_MESSAGE` FOREIGN KEY (`ChannelId`) REFERENCES `discord_channel` (`ChannelId`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `FK_USER_ID_DC_ID_MSG` FOREIGN KEY (`UserId`) REFERENCES `discord_user` (`DiscordUserId`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=latin1 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `discord_user`
--

DROP TABLE IF EXISTS `discord_user`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `discord_user` (
  `Identity` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `DiscordUserId` bigint(20) unsigned NOT NULL DEFAULT '0',
  `AccountId` int(10) DEFAULT NULL,
  `GameUserId` int(10) unsigned DEFAULT NULL,
  `AccountName` varchar(64) NOT NULL,
  `GameName` varchar(16) NOT NULL,
  `Name` varchar(64) NOT NULL,
  `Discriminator` varchar(255) NOT NULL,
  `CreatedAt` int(11) NOT NULL DEFAULT '0',
  `MessagesSent` bigint(20) unsigned NOT NULL DEFAULT '0',
  `CharactersSent` bigint(20) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`Identity`) USING BTREE,
  UNIQUE KEY `IDX_DC_ID_USER` (`DiscordUserId`),
  KEY `FK_WebAccount_DC_idx` (`AccountId`),
  KEY `FK_GameUser_DC_idx` (`GameUserId`),
  CONSTRAINT `FK_GameUser_DC` FOREIGN KEY (`GameUserId`) REFERENCES `records_user` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `FK_WebAccount_DC` FOREIGN KEY (`AccountId`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=latin1 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `log_history`
--

DROP TABLE IF EXISTS `log_history`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `log_history` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `AccountId` int(11) DEFAULT NULL COMMENT 'Only record null IF SYSTEM record',
  `Type` int(11) NOT NULL DEFAULT '0',
  `Data` bigint(20) DEFAULT '0',
  `Message` varchar(256) NOT NULL DEFAULT '',
  `Payload` text NOT NULL,
  `Time` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`Id`),
  KEY `FK_Account_Resp_idx` (`AccountId`),
  KEY `IDX_Account` (`AccountId`),
  KEY `IDX_Type` (`Type`),
  CONSTRAINT `FK_Account_Resp` FOREIGN KEY (`AccountId`) REFERENCES `accounts` (`Id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `realm`
--

DROP TABLE IF EXISTS `realm`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `realm` (
  `RealmID` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `Name` varchar(16) COLLATE utf8_bin NOT NULL,
  `AuthorityID` smallint(6) unsigned NOT NULL DEFAULT '1' COMMENT 'Authority level required',
  `GameIPAddress` varchar(45) COLLATE utf8_bin NOT NULL DEFAULT '127.0.0.1',
  `RpcIPAddress` varchar(45) COLLATE utf8_bin NOT NULL DEFAULT '127.0.0.1',
  `GamePort` int(10) unsigned NOT NULL DEFAULT '5816',
  `RpcPort` int(10) unsigned NOT NULL DEFAULT '5817',
  `Status` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `Username` varchar(255) COLLATE utf8_bin NOT NULL DEFAULT 'test',
  `Password` varchar(255) COLLATE utf8_bin NOT NULL DEFAULT 'test',
  `LastPing` datetime DEFAULT NULL,
  `DatabaseHost` varchar(255) COLLATE utf8_bin NOT NULL DEFAULT '',
  `DatabaseUser` varchar(255) COLLATE utf8_bin NOT NULL DEFAULT '',
  `DatabasePass` varchar(255) COLLATE utf8_bin NOT NULL DEFAULT '',
  `DatabaseSchema` varchar(255) COLLATE utf8_bin NOT NULL DEFAULT '',
  `DatabasePort` varchar(255) COLLATE utf8_bin NOT NULL DEFAULT '',
  PRIMARY KEY (`RealmID`) USING BTREE,
  UNIQUE KEY `RealmID_UNIQUE` (`RealmID`) USING BTREE,
  UNIQUE KEY `Name_UNIQUE` (`Name`) USING BTREE,
  KEY `fk_realm_account_authority_idx` (`AuthorityID`) USING BTREE,
  CONSTRAINT `fk_realm_account_authority` FOREIGN KEY (`AuthorityID`) REFERENCES `account_authority` (`AuthorityID`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8 COLLATE=utf8_bin ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `realms_status`
--

DROP TABLE IF EXISTS `realms_status`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `realms_status` (
  `id` int(4) unsigned NOT NULL AUTO_INCREMENT,
  `realm_id` int(4) unsigned NOT NULL,
  `realm_name` varchar(255) NOT NULL,
  `old_status` tinyint(1) unsigned NOT NULL,
  `new_status` tinyint(1) unsigned NOT NULL,
  `time` datetime NOT NULL,
  `players_online` int(4) unsigned NOT NULL DEFAULT '0',
  `max_players_online` int(4) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`) USING BTREE,
  KEY `Realms` (`realm_id`) USING BTREE,
  CONSTRAINT `Realms` FOREIGN KEY (`realm_id`) REFERENCES `realm` (`RealmID`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=6737 DEFAULT CHARSET=utf8mb4 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `records_family`
--

DROP TABLE IF EXISTS `records_family`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `records_family` (
  `Identity` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `ServerIdentity` int(10) unsigned NOT NULL DEFAULT '0',
  `FamilyIdentity` int(10) unsigned NOT NULL DEFAULT '0',
  `Name` varchar(64) NOT NULL DEFAULT '',
  `LeaderIdentity` int(10) unsigned NOT NULL DEFAULT '0',
  `Count` int(10) unsigned NOT NULL DEFAULT '0',
  `Money` bigint(20) unsigned NOT NULL DEFAULT '0',
  `CreatedAt` datetime NOT NULL,
  `DeletedAt` datetime DEFAULT NULL,
  `ChallengeMap` int(10) unsigned NOT NULL DEFAULT '0',
  `DominatedMap` int(10) unsigned NOT NULL DEFAULT '0',
  `Level` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `BpTower` tinyint(3) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`Identity`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=latin1 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `records_guild_war`
--

DROP TABLE IF EXISTS `records_guild_war`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `records_guild_war` (
  `Identity` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `ServerIdentity` int(10) unsigned NOT NULL DEFAULT '0',
  `SyndicateIdentity` int(10) unsigned NOT NULL DEFAULT '0',
  `LeaderIdentity` int(10) unsigned NOT NULL,
  `Date` datetime NOT NULL,
  PRIMARY KEY (`Identity`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=latin1 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `records_syndicate`
--

DROP TABLE IF EXISTS `records_syndicate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `records_syndicate` (
  `Id` int(4) unsigned NOT NULL AUTO_INCREMENT,
  `ServerIdentity` int(10) unsigned NOT NULL DEFAULT '0',
  `SyndicateIdentity` int(10) unsigned NOT NULL DEFAULT '0',
  `Name` varchar(16) NOT NULL,
  `LeaderIdentity` int(4) unsigned NOT NULL DEFAULT '0',
  `Count` int(4) unsigned NOT NULL DEFAULT '0',
  `CreatedAt` datetime NOT NULL,
  `DeletedAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `records_user`
--

DROP TABLE IF EXISTS `records_user`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `records_user` (
  `Id` int(4) unsigned NOT NULL AUTO_INCREMENT,
  `ServerIdentity` int(10) unsigned NOT NULL DEFAULT '0',
  `UserIdentity` int(11) unsigned NOT NULL,
  `AccountIdentity` int(10) unsigned NOT NULL DEFAULT '0',
  `Name` varchar(16) NOT NULL,
  `MateId` int(4) unsigned NOT NULL,
  `Level` tinyint(1) unsigned NOT NULL DEFAULT '1',
  `Experience` bigint(16) unsigned NOT NULL DEFAULT '0',
  `Profession` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `OldProfession` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `NewProfession` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `Metempsychosis` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `Strength` smallint(2) unsigned NOT NULL DEFAULT '0',
  `Agility` smallint(2) unsigned NOT NULL DEFAULT '0',
  `Vitality` smallint(2) unsigned NOT NULL DEFAULT '0',
  `Spirit` smallint(2) unsigned NOT NULL DEFAULT '0',
  `AdditionalPoints` smallint(2) unsigned NOT NULL DEFAULT '0',
  `SyndicateIdentity` int(4) unsigned NOT NULL DEFAULT '0',
  `SyndicatePosition` smallint(2) unsigned NOT NULL DEFAULT '0',
  `NobilityDonation` bigint(16) unsigned NOT NULL DEFAULT '0',
  `NobilityRank` tinyint(1) unsigned NOT NULL DEFAULT '0',
  `SupermanCount` int(4) unsigned NOT NULL DEFAULT '0',
  `DeletedAt` datetime DEFAULT NULL,
  `Money` bigint(20) unsigned NOT NULL DEFAULT '0',
  `WarehouseMoney` int(10) unsigned NOT NULL DEFAULT '0',
  `ConquerPoints` int(10) unsigned NOT NULL DEFAULT '0',
  `FamilyIdentity` int(10) unsigned NOT NULL DEFAULT '0',
  `FamilyRank` smallint(5) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE KEY `IdIdx` (`Id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=604 DEFAULT CHARSET=utf8 ROW_FORMAT=DYNAMIC;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `security_questions`
--

DROP TABLE IF EXISTS `security_questions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `security_questions` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Name` varchar(45) NOT NULL DEFAULT '',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `Name_UNIQUE` (`Name`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `system_config`
--

DROP TABLE IF EXISTS `system_config`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `system_config` (
  `Key` varchar(64) NOT NULL,
  `StrData0` varchar(256) DEFAULT NULL,
  `StrData1` varchar(256) DEFAULT NULL,
  `StrData2` varchar(256) DEFAULT NULL,
  `StrData3` varchar(256) DEFAULT NULL,
  `Text` text,
  `Data0` bigint(20) NOT NULL DEFAULT '0',
  `Data1` bigint(20) NOT NULL DEFAULT '0',
  `Data2` bigint(20) NOT NULL DEFAULT '0',
  `Data3` bigint(20) NOT NULL DEFAULT '0',
  `CreationDate` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` datetime DEFAULT NULL,
  `DeleteDate` datetime DEFAULT NULL,
  PRIMARY KEY (`Key`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2022-02-28 16:54:41
