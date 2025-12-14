using RevisionPlanner.Models;
using RevisionPlanner.Enums;

namespace RevisionPlanner.Services
{
    public static class RevisionPriority
    {
        // Returns: negative => a higher priority than b
        public static int Compare(Subject a, Subject b, DateTime today)
        {
            var da = DaysToExam(a, today);
            var db = DaysToExam(b, today);

            // 1) Exams in 0-2 days dominate (urgent band)
            bool aUrgent = da.HasValue && da.Value >= 0 && da.Value <= 2;
            bool bUrgent = db.HasValue && db.Value >= 0 && db.Value <= 2;

            if (aUrgent && !bUrgent) return -1;
            if (!aUrgent && bUrgent) return 1;

            // If both urgent: same time rule => higher difficulty first if same days
            if (aUrgent && bUrgent)
            {
                if (da != db) return da.Value.CompareTo(db.Value); // sooner first
                return b.Difficulty.CompareTo(a.Difficulty);       // same days => higher diff first
            }

            // 2) "Difficulty 3 slightly later than difficulty 1" rule
            // If dates are close (<= 3 days apart), let higher difficulty win,
            // except when the easy subject is "next day" (1 day away).
            if (da.HasValue && db.HasValue)
            {
                int diffDays = Math.Abs(da.Value - db.Value);

                if (diffDays <= 3)
                {
                    // Exception: difficulty 1 with exam next day outranks
                    bool aEasyNextDay = (a.Difficulty == DifficultyLevel.Easy && da.Value == 1);
                    bool bEasyNextDay = (b.Difficulty == DifficultyLevel.Easy && db.Value == 1);

                    if (aEasyNextDay && !bEasyNextDay) return -1;
                    if (!aEasyNextDay && bEasyNextDay) return 1;

                    // Otherwise higher difficulty wins when close in time
                    if (a.Difficulty != b.Difficulty)
                        return b.Difficulty.CompareTo(a.Difficulty);
                }

                // 3) General rule: sooner exam first, then difficulty
                int examCompare = da.Value.CompareTo(db.Value);
                if (examCompare != 0) return examCompare;

                return b.Difficulty.CompareTo(a.Difficulty);
            }

            // 4) If only one has an exam date: treat that as higher priority than "no date"
            if (da.HasValue && !db.HasValue) return -1;
            if (!da.HasValue && db.HasValue) return 1;

            // 5) Neither has exam date => higher difficulty first (optional)
            return b.Difficulty.CompareTo(a.Difficulty);
        }

        private static int? DaysToExam(Subject s, DateTime today)
        {
            if (!s.ExamDate.HasValue) return null;
            return (int)(s.ExamDate.Value.Date - today.Date).TotalDays;
        }
    }
}
