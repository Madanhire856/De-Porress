using System.Data;
using Microsoft.EntityFrameworkCore;
using SMS.Data;
using SMS.Lib;
namespace SMS.Web.Services;
public class PrefectAppointments(SMSDbContext db)
{
    public record Request(Guid StudentId, int PostTitleId, int StatusId, DateTime StartDate, DateTime? EndDate, Guid? NominatorId);
    public record Result(Guid? Id, string? Error, bool Missing = false);
    public static string? Validate(Request input)
    {
        if (input.StudentId == Guid.Empty) return "Select a student.";
        if (!Enum.IsDefined(typeof(PrefectPostTitle), input.PostTitleId)) return "Select a valid post title.";
        if (!Enum.IsDefined(typeof(PrefectStatus), input.StatusId)) return "Select a valid appointment status.";
        if (input.StartDate == default || !input.EndDate.HasValue) return "Enter both appointment dates.";
        if (input.EndDate.Value.Date < input.StartDate.Date) return "End date must be on or after the start date.";
        return null;
    }
    public async Task<Result> SaveAsync(Guid? id, Request input, Guid actor, Guid? nominationId = null)
    {
        var validation = Validate(input);
        if (validation != null) return new(null, validation);
        Result result = new(null, "The appointment could not be saved.");
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var entity = id.HasValue ? await db.Prefects.SingleOrDefaultAsync(x => x.Id == id) : null;
            if (id.HasValue && entity == null) { result = new(null, null, true); return; }
            PrefectNomination? nomination = null;
            if (nominationId.HasValue)
            {
                nomination = await db.PrefectNominations.SingleOrDefaultAsync(x => x.Id == nominationId);
                if (nomination == null) { result = new(null, null, true); return; }
                if (nomination.PrefectId.HasValue) { result = new(nomination.PrefectId, null); return; }
                if (id.HasValue || nomination.StudentId != input.StudentId || input.StatusId != (int)PrefectStatus.ACTIVE
                    || nomination.StatusId is not ((int)PrefectAppointmentStatus.NOMINATED or (int)PrefectAppointmentStatus.VETTING_IN_PROGRESS))
                { result = new(null, "This nomination is no longer pending or its appointment details have changed. Reload the nominations."); return; }
            }
            var student = await db.Students.SingleOrDefaultAsync(x => x.Id == input.StudentId);
            if (student == null || (input.StatusId == (int)PrefectStatus.ACTIVE && student.EnrolmentStatusId != (int)EnrolmentStatus.ACTIVE))
            { result = new(null, "Active appointments require an actively enrolled student."); return; }
            if (input.NominatorId.HasValue && !await db.Staff.AnyAsync(x => x.Id == input.NominatorId))
            { result = new(null, "The selected nominator no longer exists."); return; }
            if (input.StatusId == (int)PrefectStatus.ACTIVE && await db.Prefects.AnyAsync(x => x.StudentId == input.StudentId
                && x.Id != (id ?? Guid.Empty) && x.StatusId == (int)PrefectStatus.ACTIVE
                && x.StartDate <= input.EndDate!.Value.Date && x.EndDate >= input.StartDate.Date))
            { result = new(null, "This student already has an active appointment overlapping these dates."); return; }
            if (entity == null)
            {
                entity = new() { Id = Guid.NewGuid(), CreatorId = actor, CreationDate = DateTime.UtcNow };
                db.Prefects.Add(entity);
            }
            else if (entity.StudentId != input.StudentId)
            { result = new(null, "An existing appointment cannot be transferred to another student. Create a new appointment."); return; }
            entity.StudentId = input.StudentId; entity.PostTitleId = input.PostTitleId; entity.StatusId = input.StatusId;
            entity.StartDate = input.StartDate.Date; entity.EndDate = input.EndDate!.Value.Date; entity.NominatorId = input.NominatorId;
            if (nomination != null)
            {
                entity.NominatorId = await db.Staff.Where(x => x.UserId == nomination.NominatedByUserId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
                nomination.StatusId = (int)PrefectAppointmentStatus.APPOINTED;
                nomination.Prefect = entity; nomination.ReviewedByUserId = actor; nomination.ReviewedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(); await transaction.CommitAsync(); result = new(entity.Id, null);
        });
        return result;
    }
}
