using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TeacherScheduler
{
    public class SchoolsViewModel : IPageViewModel
    {        
        public string Name { get; private set; } = "Schools";

        public ObservableCollection<School> Schools { get; } = new ObservableCollection<School>();

        public ObservableCollection<ObservableCollection<School>> SchoolsSets { get; } = new ObservableCollection<ObservableCollection<School>>();

        public School SelectedSchool { get; set; }
        
        public ICommand AddSchoolCmd { get; }
        public ICommand RemoveSchoolCmd { get; }
        public ICommand MoveSchoolBetweenSetsCmd { get; }

        public SchoolsViewModel() 
        {
            int schoolsSetsNr = 0;
            List<Tuple<School, int>> schoolsData = DataBaseManager.Instance.loadSchoolsData();
            foreach (Tuple<School, int> schoolData in schoolsData)
            {
                Schools.Add(schoolData.Item1);                
                if (schoolsSetsNr < schoolData.Item2 + 1)
                    schoolsSetsNr = schoolData.Item2 + 1;
            }

            for (int schoolsSetIdx = 0; schoolsSetIdx < schoolsSetsNr; schoolsSetIdx++)
                SchoolsSets.Add(new ObservableCollection<School>());

            foreach (Tuple<School, int> schoolData in schoolsData)
                SchoolsSets[schoolData.Item2].Add(schoolData.Item1);
            
            AddSchoolCmd = new RelayCommand(param => addSchool(null, SchoolsSets.Count));
            RemoveSchoolCmd = new RelayCommand(param => removeSchool((School)param));
            MoveSchoolBetweenSetsCmd = new RelayCommand(param => moveSchoolBetweenSets((object[])param));
        }

        public int[,] getSchoolsDistsGraph()
        {           
            int[,] schoolsDistsGraph = new int[Schools.Count, Schools.Count];
            for (int i = 0; i < Schools.Count; i++)
            {
                for (int j = 0; j < Schools.Count; j++)
                    schoolsDistsGraph[i, j] = 1;// int.MaxValue;
            }

            foreach (ObservableCollection<School> schoolsSet in SchoolsSets)
            {
                int[] schoolsIdxs = new int[schoolsSet.Count];
                for (int s = 0; s < schoolsSet.Count; s++)
                    schoolsIdxs[s] = Schools.IndexOf(schoolsSet[s]);

                foreach (int s1 in schoolsIdxs)
                {
                    foreach (int s2 in schoolsIdxs)
                        schoolsDistsGraph[s1, s2] = 0;
                }
            }

            return schoolsDistsGraph;
        }

        private void addSchool(School school, int setIdx)
        {            
            if (school == null)
                school = new School();                    

            Schools.Add(school);
            ObservableCollection<School> schoolsSet;
            if (setIdx == SchoolsSets.Count)
            {
                schoolsSet = new ObservableCollection<School>();
                SchoolsSets.Add(schoolsSet);
            }
            else
                schoolsSet = SchoolsSets[setIdx];
            schoolsSet.Insert(0, school);      
                        
            DataBaseManager.Instance.addSchool(school, setIdx);
        }

        private int removeSchool(School schoolRemoved)
        {
            if (schoolRemoved != null)
            {                
                for (int removedSchoolSetIdx = 0; removedSchoolSetIdx < SchoolsSets.Count; removedSchoolSetIdx++)
                {
                    if (SchoolsSets[removedSchoolSetIdx].Remove(schoolRemoved)) {
                        if (SchoolsSets[removedSchoolSetIdx].Count == 0)
                        {                            
                            for (int schoolsSetIdx = removedSchoolSetIdx + 1; schoolsSetIdx < SchoolsSets.Count; schoolsSetIdx++)
                            {
                                foreach (School school in SchoolsSets[schoolsSetIdx])
                                    DataBaseManager.Instance.updateSchoolGrpIdx(school.Name, schoolsSetIdx - 1);
                            }
                            SchoolsSets.RemoveAt(removedSchoolSetIdx);
                        }
                        Schools.Remove(schoolRemoved);
                        DataBaseManager.Instance.removeSchool(schoolRemoved.Name);

                        return removedSchoolSetIdx;
                    }
                }
            }

            return -1;
        } 
        
        public void moveSchoolBetweenSets(object[] parameters)
        {
            School school = (School)parameters[0];
            int newSetIdx = (int)parameters[1];
            int setsNrBeforeMove = SchoolsSets.Count;
            if (removeSchool(school) < newSetIdx && SchoolsSets.Count < setsNrBeforeMove)
                addSchool(school, newSetIdx - 1);
            else
                addSchool(school, newSetIdx);
        }
    }
}
