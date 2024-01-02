-- 
-- This script must be ran into the account database
--

CREATE TABLE `new_account_zf`.`download_type` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(45) NOT NULL,
  `Flag` INT NOT NULL DEFAULT 0,
  `CreationDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` DATETIME NULL,
  PRIMARY KEY (`Id`));

CREATE TABLE `new_account_zf`.`download` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `DownloadTypeId` INT NOT NULL,
  `ArticleId` INT NULL DEFAULT NULL,
  `Name` VARCHAR(45) NOT NULL,
  `Description` VARCHAR(255) NOT NULL,
  `Flags` INT NOT NULL,
  `CreationDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` DATETIME NULL DEFAULT NULL,
  `DeleteDate` DATETIME NULL DEFAULT NULL,
  PRIMARY KEY (`Id`),
  INDEX `DownloadTypeIdIdx` (`DownloadTypeId` ASC),
  CONSTRAINT `FkDownloadTypeDownload`
    FOREIGN KEY (`DownloadTypeId`)
    REFERENCES `new_account_zf`.`download_type` (`Id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION);

CREATE TABLE `new_account_zf`.`download_url` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `DownloadId` INT NOT NULL,
  `Name` VARCHAR(64) NOT NULL,
  `Url` VARCHAR(255) NOT NULL,
  `Flags` INT NOT NULL DEFAULT 0,
  `CreationDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ModifiedDate` DATETIME NULL,
  `DeleteDate` DATETIME NULL,
  PRIMARY KEY (`Id`),
  INDEX `DownloadUrlDownloadIdIdx` (`DownloadId` ASC),
  CONSTRAINT `FkDownloadUrlDownload`
    FOREIGN KEY (`DownloadId`)
    REFERENCES `new_account_zf`.`download` (`Id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION);
