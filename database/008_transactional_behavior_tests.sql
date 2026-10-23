/* Stage 76: transactional behavioral tests. ONLY run against a disposable database.
   Every inserted row is rolled back. This script requires the Stage 30 relational schema. */
SET NOCOUNT ON;
SET XACT_ABORT OFF; -- Expected constraint violations must remain catchable.
IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
    THROW 51600, 'Refusing to run against a system database.', 1;
IF OBJECT_ID(N'dbo.LmsEnrollments', N'U') IS NULL OR OBJECT_ID(N'dbo.LmsGrades', N'U') IS NULL
    THROW 51601, 'Relational schema is missing.', 1;
IF @@TRANCOUNT <> 0 THROW 51602, 'Run outside an existing transaction.', 1;
DECLARE @passed int = 0;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @student int = -2100000001, @instructor int = -2100000002,
            @course int = -2100000003, @enrollment int = -2100000004;
    IF EXISTS (SELECT 1 FROM dbo.LmsStudents WHERE StudentId = @student)
       OR EXISTS (SELECT 1 FROM dbo.LmsCourses WHERE CourseId = @course)
       OR EXISTS (SELECT 1 FROM dbo.LmsEnrollments WHERE EnrollmentId = @enrollment)
       OR EXISTS (SELECT 1 FROM dbo.LmsInstructors WHERE InstructorId = @instructor)
        THROW 51603, 'Reserved fixture IDs are already in use.', 1;
    INSERT dbo.LmsStudents(StudentId,FullName,Email,Progress)
      VALUES (@student,N'Integration Test Student',N'stage76.student@example.invalid',0);
    INSERT dbo.LmsInstructors(InstructorId,FullName,Email)
      VALUES (@instructor,N'Integration Test Instructor',N'stage76.instructor@example.invalid');
    INSERT dbo.LmsCourses(CourseId,Title,InstructorId)
      VALUES (@course,N'Stage76 Integration Test Course',@instructor);
    INSERT dbo.LmsEnrollments(EnrollmentId,StudentId,CourseId)
      VALUES (@enrollment,@student,@course);
    IF NOT EXISTS (SELECT 1 FROM dbo.LmsEnrollments WHERE EnrollmentId=@enrollment AND StudentId=@student AND CourseId=@course)
        THROW 51604, 'Enrollment relationship failed.', 1;
    SET @passed += 1;
    INSERT dbo.LmsGrades(GradeId,EnrollmentId,Score) VALUES (-2100000005,@enrollment,19.50);
    IF NOT EXISTS (SELECT 1 FROM dbo.LmsGrades WHERE GradeId=-2100000005 AND Score=19.50)
        THROW 51605, 'Valid grade persistence failed.', 1;
    SET @passed += 1;
    DECLARE @rejected bit = 0;
    BEGIN TRY
        INSERT dbo.LmsEnrollments(EnrollmentId,StudentId,CourseId)
          VALUES (-2100000006,@student,@course);
    END TRY BEGIN CATCH
        IF ERROR_NUMBER() IN (2601,2627) SET @rejected=1;
        ELSE THROW;
    END CATCH;
    IF @rejected=0 THROW 51606, 'Duplicate enrollment was accepted.', 1;
    SET @passed += 1;
    SET @rejected=0;
    BEGIN TRY
        INSERT dbo.LmsGrades(GradeId,EnrollmentId,Score) VALUES (-2100000007,@enrollment,21.00);
    END TRY BEGIN CATCH
        IF ERROR_NUMBER()=547 SET @rejected=1;
        ELSE THROW;
    END CATCH;
    IF @rejected=0 THROW 51607, 'Out-of-range grade was accepted.', 1;
    SET @passed += 1;
    SET @rejected=0;
    BEGIN TRY
        INSERT dbo.LmsEnrollments(EnrollmentId,StudentId,CourseId)
          VALUES (-2100000008,-2100000009,@course);
    END TRY BEGIN CATCH
        IF ERROR_NUMBER()=547 SET @rejected=1;
        ELSE THROW;
    END CATCH;
    IF @rejected=0 THROW 51608, 'Missing student FK was accepted.', 1;
    SET @passed += 1;
    IF XACT_STATE() <> 1 THROW 51609, 'Test transaction became uncommittable.', 1;
    ROLLBACK TRANSACTION;
    SELECT N'PASS: Stage 76 transactional behavioral tests' AS Result, @passed AS PassedAssertions;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
