CREATE TABLE [dbo].[RegulatedQualificationsImportStaging]
(
	[Id]		INT IDENTITY (1, 1)            NOT NULL,
	JsonData	NVARCHAR(MAX),

	CONSTRAINT [PK_RegulatedQualificationStaging] PRIMARY KEY CLUSTERED ([Id] ASC),
)
