using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorConnect.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingStudentQuizSubmissionDateColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Student_Quiz.Submission_Date has been in the C# model (and in the EF model
            // snapshot) for a long time, but no earlier migration ever actually added the
            // column to the real Student_Quizzes table — so `dotnet ef migrations add`
            // sees no model change and generates nothing; the gap has to be added by hand.
            // This is what was throwing "Invalid column name 'Submission_Date'" on every
            // GetModuleQuizzes/SubmitQuiz call. Guarded so it's a safe no-op on any database
            // that (unlike the checked-in migration history) already happens to have it.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Student_Quizzes]') AND name = N'Submission_Date')
                    ALTER TABLE [Student_Quizzes] ADD [Submission_Date] datetime2 NOT NULL DEFAULT '1900-01-01T00:00:00';
            ");

            // Second, related schema-drift fix bundled into this same migration: Assignment_Submissions.Grade
            // and .Feedback were originally created (in 20260414170457_AddIteration2AcademicAndTutorData) as
            // NOT NULL columns named Assignment_grade_amount/Assignment_Feedback, then later renamed in-place
            // via sp_rename (20260417120000_AddStudentEnrollmentAndFileUploadSupport) to Grade/Feedback — a
            // rename preserves the NOT NULL constraint, so on any database that went through that real
            // migration history (i.e. every non-fresh database) these two columns are still NOT NULL today,
            // even though the C# model has always declared them nullable (decimal? Grade, string? Feedback)
            // and a brand-new database's guarded "ADD ... NULL" in that same migration only fires when the
            // column doesn't exist yet, so it never corrected the already-renamed columns either.
            // This silently worked as long as SubmitAssignment wrote Grade = 0, Feedback = "" for a new
            // submission; once that was changed to Grade = null, Feedback = null (so the frontend could tell
            // "never graded" apart from an actual 0%), every new assignment submission started throwing
            // "Cannot insert the value NULL into column 'Feedback'/'Grade'" — this is what was reported as
            // "submitting an assignment does not work anymore." Guarded so it's a safe no-op on any database
            // where these are already nullable (e.g. a fresh database, or if run twice).
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignment_Submissions]') AND name = N'Grade' AND is_nullable = 0)
                    ALTER TABLE [Assignment_Submissions] ALTER COLUMN [Grade] decimal(5,2) NULL;
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignment_Submissions]') AND name = N'Feedback' AND is_nullable = 0)
                    ALTER TABLE [Assignment_Submissions] ALTER COLUMN [Feedback] nvarchar(max) NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Student_Quizzes]') AND name = N'Submission_Date')
                    ALTER TABLE [Student_Quizzes] DROP COLUMN [Submission_Date];
            ");

            // Only revert Grade/Feedback back to NOT NULL if doing so wouldn't itself fail on existing NULLs
            // (there will be plenty, in any database that's accepted a submission since the Up() ran).
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignment_Submissions]') AND name = N'Grade' AND is_nullable = 1)
                   AND NOT EXISTS (SELECT 1 FROM [Assignment_Submissions] WHERE [Grade] IS NULL)
                    ALTER TABLE [Assignment_Submissions] ALTER COLUMN [Grade] decimal(5,2) NOT NULL;
            ");
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Assignment_Submissions]') AND name = N'Feedback' AND is_nullable = 1)
                   AND NOT EXISTS (SELECT 1 FROM [Assignment_Submissions] WHERE [Feedback] IS NULL)
                    ALTER TABLE [Assignment_Submissions] ALTER COLUMN [Feedback] nvarchar(max) NOT NULL;
            ");
        }
    }
}
