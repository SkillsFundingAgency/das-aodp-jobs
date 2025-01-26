CREATE TABLE [dbo].FundedQualifications
(

	[Id] INT IDENTITY(1,1) NOT NULL,
	[DateOfOfqualDataSnapshot] DATETIME NULL,
	[QualificationName] NVARCHAR(255) NULL,
	[AwardingOrganisation] NVARCHAR(255) NULL,
	[QualificationNumber] NVARCHAR(20) NULL,
	[Level] NVARCHAR(255) NULL,
	[QualificationType] NVARCHAR(255) NULL,
	[Subcategory] NVARCHAR(255) NULL,
	[SectorSubjectArea] NVARCHAR(255) NULL,
	[Status] NVARCHAR(255) NULL,
	[AwardingOrganisationURL] NVARCHAR(255) NULL,
	[QualificationNumberVarchar] VARCHAR(100) NULL,
	[ImportDate] DATETIME NOT NULL CONSTRAINT DF_FundedQualifications_CreateDate_GETDATE DEFAULT GETDATE()
	CONSTRAINT [PK_FundedQualifications] PRIMARY KEY NONCLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY])

