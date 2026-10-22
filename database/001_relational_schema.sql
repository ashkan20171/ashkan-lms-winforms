/* Stage 30: relational schema foundation. Execute in a dedicated SQL Server database.
   This script does not migrate or replace dbo.LmsState snapshot persistence. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.LmsStudents',N'U') IS NULL
CREATE TABLE dbo.LmsStudents (
 StudentId int NOT NULL CONSTRAINT PK_LmsStudents PRIMARY KEY,
 FullName nvarchar(120) NOT NULL,
 Email nvarchar(254) NOT NULL,
 StatusKey nvarchar(40) NOT NULL CONSTRAINT DF_LmsStudents_Status DEFAULT N'Active',
 Progress int NOT NULL CONSTRAINT DF_LmsStudents_Progress DEFAULT (0),
 CONSTRAINT CK_LmsStudents_Progress CHECK (Progress BETWEEN 0 AND 100),
 CONSTRAINT UQ_LmsStudents_Email UNIQUE (Email)
);
IF OBJECT_ID(N'dbo.LmsInstructors',N'U') IS NULL
CREATE TABLE dbo.LmsInstructors (
 InstructorId int NOT NULL CONSTRAINT PK_LmsInstructors PRIMARY KEY,
 FullName nvarchar(120) NOT NULL,
 Email nvarchar(254) NOT NULL,
 Expertise nvarchar(160) NULL,
 CONSTRAINT UQ_LmsInstructors_Email UNIQUE (Email)
);
IF OBJECT_ID(N'dbo.LmsCourses',N'U') IS NULL
CREATE TABLE dbo.LmsCourses (
 CourseId int NOT NULL CONSTRAINT PK_LmsCourses PRIMARY KEY,
 Title nvarchar(160) NOT NULL,
 InstructorId int NULL,
 StatusKey nvarchar(40) NOT NULL CONSTRAINT DF_LmsCourses_Status DEFAULT N'Draft',
 Progress int NOT NULL CONSTRAINT DF_LmsCourses_Progress DEFAULT (0),
 CONSTRAINT CK_LmsCourses_Progress CHECK (Progress BETWEEN 0 AND 100),
 CONSTRAINT FK_LmsCourses_Instructors FOREIGN KEY (InstructorId) REFERENCES dbo.LmsInstructors(InstructorId),
 CONSTRAINT UQ_LmsCourses_Title UNIQUE (Title)
);
IF OBJECT_ID(N'dbo.LmsEnrollments',N'U') IS NULL
CREATE TABLE dbo.LmsEnrollments (
 EnrollmentId int NOT NULL CONSTRAINT PK_LmsEnrollments PRIMARY KEY,
 StudentId int NOT NULL,
 CourseId int NOT NULL,
 EnrolledAt datetime2(0) NOT NULL CONSTRAINT DF_LmsEnrollments_Date DEFAULT SYSUTCDATETIME(),
 StatusKey nvarchar(40) NOT NULL CONSTRAINT DF_LmsEnrollments_Status DEFAULT N'Active',
 CONSTRAINT FK_LmsEnrollments_Students FOREIGN KEY (StudentId) REFERENCES dbo.LmsStudents(StudentId),
 CONSTRAINT FK_LmsEnrollments_Courses FOREIGN KEY (CourseId) REFERENCES dbo.LmsCourses(CourseId),
 CONSTRAINT UQ_LmsEnrollments_StudentCourse UNIQUE (StudentId,CourseId)
);
IF OBJECT_ID(N'dbo.LmsAttendance',N'U') IS NULL
CREATE TABLE dbo.LmsAttendance (
 AttendanceId int NOT NULL CONSTRAINT PK_LmsAttendance PRIMARY KEY,
 EnrollmentId int NOT NULL,
 SessionDate date NOT NULL,
 StatusKey nvarchar(40) NOT NULL,
 CONSTRAINT FK_LmsAttendance_Enrollments FOREIGN KEY (EnrollmentId) REFERENCES dbo.LmsEnrollments(EnrollmentId),
 CONSTRAINT UQ_LmsAttendance_EnrollmentDate UNIQUE (EnrollmentId,SessionDate)
);
IF OBJECT_ID(N'dbo.LmsGrades',N'U') IS NULL
CREATE TABLE dbo.LmsGrades (
 GradeId int NOT NULL CONSTRAINT PK_LmsGrades PRIMARY KEY,
 EnrollmentId int NOT NULL,
 Score decimal(5,2) NOT NULL,
 Letter nvarchar(8) NULL,
 CONSTRAINT FK_LmsGrades_Enrollments FOREIGN KEY (EnrollmentId) REFERENCES dbo.LmsEnrollments(EnrollmentId),
 CONSTRAINT CK_LmsGrades_Score CHECK (Score BETWEEN 0 AND 20)
);
IF OBJECT_ID(N'dbo.LmsCertificates',N'U') IS NULL
CREATE TABLE dbo.LmsCertificates (
 CertificateId int NOT NULL CONSTRAINT PK_LmsCertificates PRIMARY KEY,
 EnrollmentId int NOT NULL,
 IssuedAt datetime2(0) NOT NULL,
 CertificateCode nvarchar(80) NOT NULL,
 CONSTRAINT FK_LmsCertificates_Enrollments FOREIGN KEY (EnrollmentId) REFERENCES dbo.LmsEnrollments(EnrollmentId),
 CONSTRAINT UQ_LmsCertificates_Code UNIQUE (CertificateCode)
);
COMMIT TRANSACTION;
