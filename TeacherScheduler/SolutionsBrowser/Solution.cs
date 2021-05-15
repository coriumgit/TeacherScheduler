using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeacherScheduler
{
    public class Solution
    {
        public struct Placement
        {
            public Student student;
            public int hourIdx;
            public int dayIdx;
        }

        public ObservableMatrix<Student> Schedule { get; private set; }

        public Solution(Student[,] solutionMatrix) : this()
        {            
            Schedule.IsObserved = false;
            for (int hourIdx = 0; hourIdx < solutionMatrix.GetLength(0); hourIdx++)
            {
                for (int dayIdx = 0; dayIdx < solutionMatrix.GetLength(1); dayIdx++)                
                    Schedule[hourIdx][dayIdx] = solutionMatrix[hourIdx,dayIdx];                
            }
            Schedule.IsObserved = true;
        }

        public Solution(List<Placement> placements) : this()
        {            
            Schedule.IsObserved = false;
            foreach (Placement placment in placements)
                Schedule[placment.hourIdx][placment.dayIdx] = placment.student;
            Schedule.IsObserved = true;
        }

        private Solution()
        {
            Schedule = new ObservableMatrix<Student>(App.HOURS_NR, App.DAYS_LABELS.Length);
        }
    }
}
