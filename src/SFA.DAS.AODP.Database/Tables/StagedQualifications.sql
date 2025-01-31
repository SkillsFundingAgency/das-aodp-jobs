CREATE TABLE [dbo].[StagedQualifications]
(
	[Id]			INT IDENTITY (1, 1)            NOT NULL,
	JsonData		NVARCHAR(MAX),
	CreatedDate		DATETIME					   DEFAULT GETDATE(),

	CONSTRAINT [PK_RegulatedQualificationStaging] PRIMARY KEY CLUSTERED ([Id] ASC),
)
