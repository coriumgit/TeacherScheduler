using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TeacherScheduler
{
    public class SchedulingSolver
    {        
        public delegate void OnSolutionProgress(int progress);
        public delegate void OnSolutionDone(List<int[,]> solutions, bool wasCanceled);

        private delegate double ScoreFunc(uint gapsNr);

        private class ProgressReporter
        {
            private BackgroundWorker bkgWorker;
            private double[] progressionRatePerStudent;
            private double progress = 0.0f;

            public ProgressReporter(BackgroundWorker bkgWorker, StudentData[] studentsData)
            {
                this.bkgWorker = bkgWorker;
                int studentsNr = studentsData.Length;
                progressionRatePerStudent = new double[studentsNr];

                double studentsAssignmentsCombosNrOverall = 1;
                foreach (StudentData studentData in studentsData)
                    studentsAssignmentsCombosNrOverall = studentsAssignmentsCombosNrOverall * studentData.assignmentsCombosNr;

                progressionRatePerStudent[studentsNr - 1] = 100.0 / studentsAssignmentsCombosNrOverall;
                for (int studentIdx = studentsNr - 2; studentIdx >= 0; studentIdx--)
                    progressionRatePerStudent[studentIdx] = progressionRatePerStudent[studentIdx + 1] * studentsData[studentIdx + 1].assignmentsCombosNr;
            }

            public void addProgress(int studentIdx, int hoursCombosProcessedNr)
            {
                double prevProgress = progress;
                progress += progressionRatePerStudent[studentIdx] * hoursCombosProcessedNr;
                if (Math.Floor(progress) > Math.Floor(prevProgress))
                    bkgWorker.ReportProgress((int)Math.Round(progress));
            }
        }

        private class SchoolsDistsConstraint
        {
            public enum SchoolTravelEnd { ORIGIN, DESTINATION }

            private int[,] schoolsDistsGraph;
            public int OriginSchoolIdx { get; internal set; } = 0;
            public int OriginSchoolDistMax { get; internal set; } = int.MaxValue / 2; // to represent infinity
            public int DestSchoolIdx { get; internal set; } = 0;
            public int DestSchoolDistMax { get; internal set; } = int.MaxValue / 2; // to represent infinity

            public SchoolsDistsConstraint(int[,] schoolsDistsGraph) 
            { 
                this.schoolsDistsGraph = schoolsDistsGraph; 
            }

            public SchoolsDistsConstraint(int[,] schoolsDistsGraph, int originSchoolIdx, int originSchoolDistMax, int destSchoolIdx, int destSchoolDistMax)
            {
                this.schoolsDistsGraph = schoolsDistsGraph;
                OriginSchoolIdx = originSchoolIdx;
                OriginSchoolDistMax = originSchoolDistMax;
                DestSchoolIdx = destSchoolIdx;
                DestSchoolDistMax = destSchoolDistMax;
            }

            public SchoolsDistsConstraint(int[,] schoolsDistsGraph, int schoolIdx, int schoolDistMax, SchoolTravelEnd schoolTravelEnd)
            {
                this.schoolsDistsGraph = schoolsDistsGraph;
                if (schoolTravelEnd == SchoolTravelEnd.ORIGIN)
                {
                    OriginSchoolIdx = schoolIdx;
                    OriginSchoolDistMax = schoolDistMax;
                }
                else
                {
                    DestSchoolIdx = schoolIdx;
                    DestSchoolDistMax = schoolDistMax;
                }
            }

            public SchoolsDistsConstraint(SchoolsDistsConstraint originConstraint, int destSchoolIdx, int destSchoolDistMax)
            {
                schoolsDistsGraph = originConstraint.schoolsDistsGraph;
                OriginSchoolIdx = originConstraint.OriginSchoolIdx;
                OriginSchoolDistMax = originConstraint.OriginSchoolDistMax;
                DestSchoolIdx = destSchoolIdx;
                DestSchoolDistMax = destSchoolDistMax;
            }

            public SchoolsDistsConstraint(int originSchoolIdx, int originSchoolDistMax, SchoolsDistsConstraint destConstraint)
            {
                schoolsDistsGraph = destConstraint.schoolsDistsGraph;
                OriginSchoolIdx = originSchoolIdx;
                OriginSchoolDistMax = originSchoolDistMax;
                DestSchoolIdx = destConstraint.DestSchoolIdx;
                DestSchoolDistMax = destConstraint.DestSchoolDistMax;
            }            

            public bool testSchool(int schoolIdx)
            {
                return schoolsDistsGraph[schoolIdx, OriginSchoolIdx] <= OriginSchoolDistMax &&
                       schoolsDistsGraph[schoolIdx, DestSchoolIdx] <= DestSchoolDistMax;
            }
        }

        private struct ScheduleTime
        {
            public int hourIdx;
            public int dayIdx;
        }

        private struct StudentData
        {
            public int id;
            public int schoolIdx;
            public int requiredHours;
            public ScheduleTime[] assignableHours;
            public int assignmentsCombosNr;                  
        }

        private const int SOLUTIONS_REQUIRED_NR_MAX = 1000;
        private static readonly ScoreFunc gapsNrScore = (uint gapsNr) => { return 1.0 * gapsNr; };
        private static readonly ScoreFunc freeDaysNrScore = (uint freeDaysNr) => { return 5.0 * freeDaysNr; };

#if DEBUG
        Logger logger = new Logger("scheduling_solver_debug.txt");
#endif

        private int hoursNr;
        private int daysNr;
        private int studentsNr;
        private ProgressReporter progressReporter = null;
        private StudentData[] studentsData;                
        private int[,] schoolsDistsGraph;
        private List<int[,]> solutions = new List<int[,]>(SOLUTIONS_REQUIRED_NR_MAX);
        private List<double> solutionsScores = new List<double>(SOLUTIONS_REQUIRED_NR_MAX);
        private double solutionScoreWorst = 0.0;
        private bool isSolving = false;
        private BackgroundWorker bkgWorker;        

        public SchedulingSolver(bool[][,] studentsAvailabilities, int[] studentsRequiredHours, int[] studentsSchoolsIdxs, int[,] schoolsDistsGraph, bool[,] teacherAvailability)
        {
            hoursNr = teacherAvailability.GetLength(0);
            daysNr = teacherAvailability.GetLength(1);
            
            studentsNr = studentsAvailabilities.Length;
            studentsData = new StudentData[studentsNr];
            int studentsAssignmentsCombosNrOverall = 1;
            for (int studentIdx = 0; studentIdx < studentsAvailabilities.Length; studentIdx++)
            {
                studentsData[studentIdx].id = studentIdx + 1;
                studentsData[studentIdx].schoolIdx = studentsSchoolsIdxs[studentIdx];
                studentsData[studentIdx].requiredHours = studentsRequiredHours[studentIdx];
                List <ScheduleTime> studentAssignableHours = new List<ScheduleTime>();
                bool[,] studentAvailability = studentsAvailabilities[studentIdx];
                int studentTeacherCompatibleHoursNr = 0;
                for (int dayIdx = 0; dayIdx < daysNr; dayIdx++) 
                {
                    for (int hourIdx = 0; hourIdx < hoursNr; hourIdx++)
                    {
                        if (studentAvailability[hourIdx, dayIdx] &= teacherAvailability[hourIdx, dayIdx])
                        {
                            studentAssignableHours.Add(new ScheduleTime { hourIdx = hourIdx, dayIdx = dayIdx });
                            studentTeacherCompatibleHoursNr++;
                        }
                    }
                }

                studentsData[studentIdx].assignableHours = studentAssignableHours.ToArray();
                studentsData[studentIdx].assignmentsCombosNr = nChooseK(studentTeacherCompatibleHoursNr, studentsRequiredHours[studentIdx]);
                studentsAssignmentsCombosNrOverall *= studentsData[studentIdx].assignmentsCombosNr;
            }

            this.schoolsDistsGraph = new int[schoolsDistsGraph.GetLength(0), schoolsDistsGraph.GetLength(1)];
            Array.Copy(schoolsDistsGraph, this.schoolsDistsGraph, schoolsDistsGraph.Length);

            bkgWorker = new BackgroundWorker();
        }

        public void solve(OnSolutionProgress onSolutionProgress, OnSolutionDone onSolutionDone)
        {
            if (isSolving)
                return;
            
            bkgWorker.WorkerReportsProgress = true;
            bkgWorker.DoWork += doSolve;
            bkgWorker.ProgressChanged += new ProgressChangedEventHandler(
                delegate (object o, ProgressChangedEventArgs args) { onSolutionProgress(args.ProgressPercentage); }
            );
            bkgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
                delegate (object o, RunWorkerCompletedEventArgs args) { isSolving = false; onSolutionDone(solutions, args.Cancelled); }
            );
            bkgWorker.WorkerSupportsCancellation = true;

            // solution search algo specifics
            int[] studentsAssignmentsCombosNr = new int[studentsNr];
            for (int studentIdx = 0; studentIdx < studentsNr; studentIdx++)
                studentsAssignmentsCombosNr[studentIdx] = studentsData[studentIdx].assignmentsCombosNr;
            Array.Sort(studentsAssignmentsCombosNr, studentsData);
            progressReporter = new ProgressReporter(bkgWorker, studentsData);

            isSolving = true;
            bkgWorker.RunWorkerAsync();             
        }

        public void stopSolutionsSearch()
        {
            if (!isSolving)
                return;

            bkgWorker.CancelAsync();            
        }

        private void doSolve(object o, DoWorkEventArgs args)
        {                                    
            solutions.Clear();
            int[,] scheduleCurr = new int[hoursNr, daysNr];
            SchoolsDistsConstraint[,] schoolsConstraintTable = new SchoolsDistsConstraint[hoursNr, daysNr];
            for (int hourIdx = 0; hourIdx < hoursNr; hourIdx++)
            {
                for (int dayIdx = 0; dayIdx < daysNr; dayIdx++)
                    schoolsConstraintTable[hourIdx, dayIdx] = new SchoolsDistsConstraint(schoolsDistsGraph);
            }
            args.Cancel = !doSolveRecurs(0, scheduleCurr, schoolsConstraintTable);
#if DEBUG
            logger.write();
#endif
        }

        // return value: was search finished with all possible combinations tested
        //               (will be false only if the search was canceled via stopSolutionsSearch())
        private bool doSolveRecurs(int studentIdx, int[,] scheduleCurr, SchoolsDistsConstraint[,] schoolsConstraintsTable)
        {
            if (studentIdx == studentsNr)
            {
                double scheduleCurrScore = scoreSolution(scheduleCurr);
                if (scheduleCurrScore > solutionScoreWorst)
                {
                    int insertionPos = 0;
                    while (insertionPos < solutionsScores.Count && solutionsScores[insertionPos] > scheduleCurrScore)
                        insertionPos++;
                    
                    if (solutions.Count == SOLUTIONS_REQUIRED_NR_MAX)
                    {
                        solutions.RemoveAt(SOLUTIONS_REQUIRED_NR_MAX - 1);
                        solutionsScores.RemoveAt(SOLUTIONS_REQUIRED_NR_MAX - 1);
                    }

                    solutions.Insert(insertionPos, (int[,])scheduleCurr.Clone());
                    solutionsScores.Insert(insertionPos, scheduleCurrScore);
                    solutionScoreWorst = solutionsScores[solutionsScores.Count - 1];
                }
                else if (solutions.Count < SOLUTIONS_REQUIRED_NR_MAX)
                {
                    solutions.Add((int[,])scheduleCurr.Clone());
                    solutionsScores.Add(scheduleCurrScore);
                    solutionScoreWorst = scheduleCurrScore;
                }

                return true;
            }

            if (studentsData[studentIdx].requiredHours == 0)
            {
                return doSolveRecurs(studentIdx + 1, scheduleCurr, schoolsConstraintsTable);
            }
            else
            {            
                List<ScheduleTime> assignableHoursList = new List<ScheduleTime>();
                foreach (ScheduleTime scheduleTime in studentsData[studentIdx].assignableHours)
                {
                    if (isScheduleTimeValid(studentIdx, scheduleTime, scheduleCurr, schoolsConstraintsTable))
                        assignableHoursList.Add(scheduleTime);
                }
                int requiredHoursNr = studentsData[studentIdx].requiredHours;
                int combosNr = nChooseK(assignableHoursList.Count, requiredHoursNr);
                progressReporter.addProgress(studentIdx, studentsData[studentIdx].assignmentsCombosNr - combosNr);
                if (combosNr == 0)            
                    return true;
                            
                ScheduleTime[] assignableHours = assignableHoursList.ToArray();                        
                int[] assignedHoursIdxs = new int[requiredHoursNr];
                for (int requiredHourIdx = 0; requiredHourIdx < requiredHoursNr - 1; requiredHourIdx++)
                    assignedHoursIdxs[requiredHourIdx] = requiredHourIdx;
                assignedHoursIdxs[requiredHoursNr - 1] = requiredHoursNr - 2;

                int lastAssignableHoursBlockStartIdx = assignableHours.Length - (requiredHoursNr - 1);
                for (int comboIdx = 0; comboIdx < combosNr; comboIdx++)
                {
                    int requiredHourIt = requiredHoursNr - 1;
                    while (0 <= requiredHourIt && assignedHoursIdxs[requiredHourIt] + 1 == lastAssignableHoursBlockStartIdx + requiredHourIt)
                        requiredHourIt--;

                    if (0 <= requiredHourIt)
                    {
                        assignedHoursIdxs[requiredHourIt]++;
                        requiredHourIt++;
                        for (; requiredHourIt < requiredHoursNr; requiredHourIt++)
                            assignedHoursIdxs[requiredHourIt] = assignedHoursIdxs[requiredHourIt - 1] + 1;
                    }

#if DEBUG
                    if (studentIdx == studentsNr - 1)
                    {
                        string comboLine = string.Empty;
                        foreach (int ii in assignedHoursIdxs)
                            comboLine += ii;

                        logger.appendLine("combo #" + comboIdx + ": " + comboLine);
                    }
#endif

                    ScheduleTime[] assignedHours = new ScheduleTime[requiredHoursNr];
                    for (int ii = 0; ii < requiredHoursNr; ii++)
                        assignedHours[ii] = assignableHours[assignedHoursIdxs[ii]];

                    if (isScheduleTimesComboValid(studentIdx, assignedHours))
                    {
                        assignScheduleTimes(studentIdx, assignedHours, scheduleCurr, schoolsConstraintsTable);
                        if (bkgWorker.CancellationPending) return false;
                        doSolveRecurs(studentIdx + 1, scheduleCurr, schoolsConstraintsTable);
                        if (bkgWorker.CancellationPending) return false;
                        removeScheduleTimes(assignedHours, scheduleCurr, schoolsConstraintsTable);
                    }
                
                    if (studentIdx == studentsNr - 1)
                        progressReporter.addProgress(studentIdx, 1);
                }

                return true;
            }            
        }

        private bool isScheduleTimeValid(int studentIdx, ScheduleTime scheduleTime, int[,] scheduleCurr, SchoolsDistsConstraint[,] schoolsConstraintsTable)
        {
            return scheduleCurr[scheduleTime.hourIdx, scheduleTime.dayIdx] == 0 && 
                   schoolsConstraintsTable[scheduleTime.hourIdx, scheduleTime.dayIdx].testSchool(studentsData[studentIdx].schoolIdx);
        }

        private bool isScheduleTimesComboValid(int studentIdx, ScheduleTime[] scheduleTimes)
        {
            if (scheduleTimes.Length == 1)
                return true;

            List<int>[] scheduledHoursPerDay = new List<int>[daysNr];
            int scheduledDaysNr = 0;
            foreach (ScheduleTime scheduleTime in scheduleTimes)
            {
                int scheduleDayIdx = scheduleTime.dayIdx;
                if (scheduledHoursPerDay[scheduleDayIdx] == null)
                {
                    scheduledHoursPerDay[scheduleDayIdx] = new List<int>();
                    scheduledDaysNr++;
                }
                scheduledHoursPerDay[scheduleDayIdx].Add(scheduleTime.hourIdx);
            }
            
            if (studentsData[studentIdx].requiredHours == 2 && scheduledDaysNr == 1)
                return false;
                        
            
            //for (int dayIdx = 0; dayIdx < daysNr; dayIdx++)
            //{
            //    List<int> scheduledHoursIdxs = scheduledHoursPerDay[dayIdx];
            //    if (scheduledHoursIdxs != null)
            //    {
            //        if (scheduledHoursIdxs.Count > 2)
            //            return false;
            //        else if (scheduledHoursIdxs.Count == 2)
            //        {
                        // testing if the hours are contingent for two hours
            //            if (Math.Abs(scheduledHoursIdxs[0] - scheduledHoursIdxs[1]) != 1)
            //                return false;
                        /* testing if the hours are contingent for any number of hours                          

                        int hourIdxMin = scheduledHoursIdxs[0];
                        int hourIdxMax = scheduledHoursIdxs[0];
                        for (int ii = 1; ii < scheduledHoursIdxs.Count; ii++)
                        {
                            int scheduledHourIdx = scheduledHoursIdxs[ii];
                            if (scheduledHourIdx < hourIdxMin)
                                hourIdxMin = scheduledHourIdx;
                            else if (scheduledHourIdx > hourIdxMax)
                                hourIdxMax = scheduledHourIdx;
                        }

                        if (hourIdxMax - hourIdxMin + 1 > scheduledHoursIdxs.Count)
                            return false;
                        */
             //       }
             //   }
             //}
            
            return true;
        }

        private void assignScheduleTimes(int studentIdx, ScheduleTime[] scheduleTimes, int[,] scheduleCurr, SchoolsDistsConstraint[,] schoolsConstraintsTable)
        {
            foreach (ScheduleTime scheduleTime in scheduleTimes)
            {
                //assignScheduleTime(studentIdx, assignableHours[assignedHourIdx], scheduleCurr, schoolsConstraintsTable);                
                scheduleCurr[scheduleTime.hourIdx, scheduleTime.dayIdx] = studentsData[studentIdx].id;

                int hoursIt = scheduleTime.hourIdx;
                do
                {
                    schoolsConstraintsTable[hoursIt, scheduleTime.dayIdx] = new SchoolsDistsConstraint(schoolsConstraintsTable[hoursIt, scheduleTime.dayIdx], studentsData[studentIdx].schoolIdx, scheduleTime.hourIdx - hoursIt - 1);
                    hoursIt--;
                } while (0 <= hoursIt && scheduleCurr[hoursIt, scheduleTime.dayIdx] == 0);

                hoursIt = scheduleTime.hourIdx;
                do
                {
                    schoolsConstraintsTable[hoursIt, scheduleTime.dayIdx] = new SchoolsDistsConstraint(studentsData[studentIdx].schoolIdx, hoursIt - scheduleTime.hourIdx - 1, schoolsConstraintsTable[hoursIt, scheduleTime.dayIdx]);
                    hoursIt++;
                } while (hoursIt < schoolsConstraintsTable.GetLength(0) && scheduleCurr[hoursIt, scheduleTime.dayIdx] == 0);
            }
        }

        private void removeScheduleTimes(ScheduleTime[] scheduleTimes, int[,] scheduleCurr, SchoolsDistsConstraint[,] schoolsConstraintsTable)
        {
            foreach (ScheduleTime scheduleTime in scheduleTimes)
            {
                //removeScheduleTime(assignableHours[assignedHourIdx], scheduleCurr, schoolsConstraintsTable);                
                scheduleCurr[scheduleTime.hourIdx, scheduleTime.dayIdx] = 0;

                SchoolsDistsConstraint refConstraint;
                if (scheduleTime.hourIdx < schoolsConstraintsTable.GetLength(0) - 1)
                    refConstraint = schoolsConstraintsTable[scheduleTime.hourIdx + 1, scheduleTime.dayIdx];
                else
                    refConstraint = new SchoolsDistsConstraint(schoolsDistsGraph);
                int hoursIt = scheduleTime.hourIdx;
                do
                {
                    schoolsConstraintsTable[hoursIt, scheduleTime.dayIdx] = new SchoolsDistsConstraint(schoolsConstraintsTable[hoursIt, scheduleTime.dayIdx], refConstraint.DestSchoolIdx, refConstraint.DestSchoolDistMax + scheduleTime.hourIdx - hoursIt + 1);
                    hoursIt--;
                } while (0 <= hoursIt && scheduleCurr[hoursIt, scheduleTime.dayIdx] == 0);


                if (0 < scheduleTime.hourIdx)
                    refConstraint = schoolsConstraintsTable[scheduleTime.hourIdx - 1, scheduleTime.dayIdx];
                else
                    refConstraint = new SchoolsDistsConstraint(schoolsDistsGraph);
                hoursIt = scheduleTime.hourIdx;
                do
                {
                    schoolsConstraintsTable[hoursIt, scheduleTime.dayIdx] = new SchoolsDistsConstraint(refConstraint.OriginSchoolIdx, refConstraint.OriginSchoolDistMax + hoursIt - scheduleTime.hourIdx + 1, schoolsConstraintsTable[hoursIt, scheduleTime.dayIdx]);
                    hoursIt++;
                } while (hoursIt < schoolsConstraintsTable.GetLength(0) && scheduleCurr[hoursIt, scheduleTime.dayIdx] == 0);                
            }
        }

        private double scoreSolution(int[,] solution)
        {
            //return 0;
            double gapsPenalty = 0;
            uint freeDaysNr = 0;
            for (int dayIdx = 0; dayIdx < daysNr; dayIdx++)
            {
                List<Tuple<int, int>> daySequences = new List<Tuple<int, int>>();
                int hourIdx = 0;
                int sequenceLen = 1;                
                do
                {
                    if (solution[hourIdx, dayIdx] == solution[hourIdx + 1, dayIdx])
                        sequenceLen++;
                    else
                    {
                        daySequences.Add(new Tuple<int, int>(solution[hourIdx, dayIdx], sequenceLen));
                        sequenceLen = 1;
                    }

                    hourIdx++;
                }
                while (hourIdx < hoursNr - 1);
                daySequences.Add(new Tuple<int, int>(solution[hourIdx, dayIdx], sequenceLen));

                if (daySequences.Count == 1) {
                    if (daySequences[0].Item1 == 0)
                        freeDaysNr++;
                }
                else { 
                    uint gapsNr = 0;
                    for (int sequenceIdx = 1; sequenceIdx < daySequences.Count - 1; sequenceIdx++)
                    {
                        if (daySequences[sequenceIdx].Item1 == 0)
                            gapsNr++;
                    }

                    gapsPenalty -= gapsNrScore(gapsNr);
                }
            }

            return gapsPenalty + freeDaysNrScore(freeDaysNr);
        }

        private static int nChooseK(int n, int k)
        {
            if (n < k)
                return 0;

            int res = 1;
            for (int i = 0; i < k; ++i)
            {
                res *= n - i;
                res /= i + 1;
            }

            return res;
        }

        /*
        private static T[] getArrArrangedByIndices<T>(T[] arr, int[] indices)
        {
            T[] arranged = new T[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                arranged[i] = arr[indices[i]];

            return arranged;
        }
        */
    }            
}
