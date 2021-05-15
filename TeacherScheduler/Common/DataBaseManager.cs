using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Data;
using System.Xml.Linq;

namespace TeacherScheduler
{
    public class DataBaseManager
    {
        private static DataBaseManager instance;
        public static DataBaseManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new DataBaseManager();

                return instance;
            }            
        }

        private XDocument dataXmlDoc;
        private XElement studentsXmlLmnt;        
        private XElement teacherXmlLmnt;
        private XElement schoolsXmlLmnt;

        private const string DATA_XML_FILE_NAME = "DataXML.xml";
        private const string LABEL_ROOT_NODE = "SchedulerData";
        private const string LABEL_SCHEDULES_NODE = "Schedules";
        private const string LABEL_SCHOOLS_NODE = "Schools";
        private const string LABEL_SCHOOL_NODE = "School";
        private const string LABEL_SCHOOL_NODE_SCHOOLS_SET_IDX_ATTRIB = "SchoolsSetIdx";
        private const string LABEL_STUDENTS_NODE = "Students";
        private const string LABEL_STUDENT_NODE = "Student";
        private const string LABEL_STUDENT_NODE_NAME_ATTRIB = "Name";
        private const string LABEL_STUDENT_NODE_SCHOOL_IDX_ATTRIB = "SchoolIdx";
        private const string LABEL_STUDENT_NODE_REQUIRED_HOURS_ATTRIB = "RequiredHours";
        private const string LABEL_TEACHER_NODE = "Teacher";
        private const string LABEL_HOUR_NODE = "Hour";
        private const string LABEL_HOUR_NODE_HOUR_IDX_ATTRIB = "Idx";
        private const string LABEL_DAY_NODE = "Day";
        private const string LABEL_DAY_NODE_NAME_ATTRIB = "Name";

        private DataBaseManager()
        {
            dataXmlDoc = XDocument.Load(DATA_XML_FILE_NAME);
            XElement rootLmnt = dataXmlDoc.Element(LABEL_ROOT_NODE);
            XElement schedulesLmnt = rootLmnt.Element(LABEL_SCHEDULES_NODE);
            studentsXmlLmnt = schedulesLmnt.Element(LABEL_STUDENTS_NODE);
            teacherXmlLmnt = schedulesLmnt.Element(LABEL_TEACHER_NODE);
            schoolsXmlLmnt = rootLmnt.Element(LABEL_SCHOOLS_NODE);
        }

        public void addStudent(Student student)
        {
            XElement studentLmnt = new XElement(LABEL_STUDENT_NODE);
            studentLmnt.Add(new XAttribute(LABEL_STUDENT_NODE_NAME_ATTRIB, student.Name));            
            studentLmnt.Add(new XAttribute(LABEL_STUDENT_NODE_SCHOOL_IDX_ATTRIB, student.SchoolAttended != null ? student.SchoolAttended.Name : string.Empty));
            studentLmnt.Add(new XAttribute(LABEL_STUDENT_NODE_REQUIRED_HOURS_ATTRIB, student.RequiredHoursNr));            
            for (int hourIdx = 0; hourIdx < App.HOURS_NR; hourIdx++)
            {
                XElement hourLmnt = new XElement(LABEL_HOUR_NODE);
                hourLmnt.Add(new XAttribute(LABEL_HOUR_NODE_HOUR_IDX_ATTRIB, hourIdx));
                for (int dayIdx = 0; dayIdx < App.DAYS_LABELS.Length; dayIdx++)
                {
                    XElement dayLmnt = new XElement(LABEL_DAY_NODE, student.Schedule[hourIdx][dayIdx]);
                    dayLmnt.Add(new XAttribute(LABEL_DAY_NODE_NAME_ATTRIB, App.DAYS_LABELS[dayIdx]));
                    hourLmnt.Add(dayLmnt);
                }
                studentLmnt.Add(hourLmnt);
            }
            studentsXmlLmnt.Add(studentLmnt);

            saveData();
        }

        public void updateStudentName(string oldName, string newName)
        {
            findStudentLmntByName(oldName).Attribute(LABEL_STUDENT_NODE_NAME_ATTRIB).SetValue(newName);
            saveData();
        }

        public void updateStudentSchool(string studentName, School school)
        {
            findStudentLmntByName(studentName).Attribute(LABEL_STUDENT_NODE_SCHOOL_IDX_ATTRIB).SetValue(school != null ? school.Name : string.Empty);
            saveData();
        }

        public void updateStudentRequiredHours(string studentName, int requiredHours)
        {
            findStudentLmntByName(studentName).Attribute(LABEL_STUDENT_NODE_REQUIRED_HOURS_ATTRIB).SetValue(requiredHours);
            saveData();
        }

        public void updateStudentSchedule(string name, int hourIdx, int dayIdx, bool isAvailable)
        {
            updateScheduleTime(findStudentLmntByName(name), hourIdx, dayIdx, isAvailable);
            saveData();
        }

        public void updateTeacherSchedule(int hourIdx, int dayIdx, bool isAvailable)
        {
            updateScheduleTime(teacherXmlLmnt, hourIdx, dayIdx, isAvailable);
            saveData();
        }

        public void removeStudent(string studentName)
        {
            findStudentLmntByName(studentName).Remove();
            saveData();
        }

        public List<Student> loadStudents()
        {
            List<Student> list = new List<Student>();
            foreach (XElement studentLmnt in studentsXmlLmnt.Descendants(LABEL_STUDENT_NODE))
            {
                string schoolAttendedName = (string)studentLmnt.Attribute(LABEL_STUDENT_NODE_SCHOOL_IDX_ATTRIB);
                Student student = new Student((string)studentLmnt.Attribute(LABEL_STUDENT_NODE_NAME_ATTRIB),
                                              schoolAttendedName != string.Empty ? new School(schoolAttendedName) : null,
                                              (int)studentLmnt.Attribute(LABEL_STUDENT_NODE_REQUIRED_HOURS_ATTRIB));
                loadSchedule(studentLmnt, student.Schedule);              
                list.Add(student);
            }

            return list;
        }

        public Teacher loadTeacher()
        {
            Teacher teacher = new Teacher();
            loadSchedule(teacherXmlLmnt, teacher.Schedule);
            
            return teacher;
        }

        public void addSchool(School school, int schoolsGrpIdx)
        {
            XElement schoolLmnt = new XElement(LABEL_SCHOOL_NODE);
            schoolLmnt.Add(new XAttribute(LABEL_SCHOOL_NODE_SCHOOLS_SET_IDX_ATTRIB, schoolsGrpIdx));
            schoolLmnt.SetValue(school.Name);
            schoolsXmlLmnt.Add(schoolLmnt);
            saveData();
        }

        public void updateSchoolName(string oldName, string newName)
        {
            findSchoolLmntByName(oldName).SetValue(newName);
            saveData();
        }

        public void updateSchoolGrpIdx(string schoolName, int grpIdx)
        {
            findSchoolLmntByName(schoolName).SetAttributeValue(LABEL_SCHOOL_NODE_SCHOOLS_SET_IDX_ATTRIB, grpIdx);
            saveData();
        }

        public void removeSchool(string schoolName)
        {
            findSchoolLmntByName(schoolName).Remove();
            saveData();
        }

        public List<Tuple<School, int>> loadSchoolsData()
        {
            List<Tuple<School, int>> res = new List<Tuple<School, int>>();
            foreach (XElement schoolNode in schoolsXmlLmnt.Descendants())
                res.Add(new Tuple<School, int>(new School(schoolNode.Value), int.Parse(schoolNode.Attribute(LABEL_SCHOOL_NODE_SCHOOLS_SET_IDX_ATTRIB).Value)));

            return res;
        }

        public void saveData()
        {
            dataXmlDoc.Save(DATA_XML_FILE_NAME);
        }

        private void loadSchedule(XElement agentLmnt, ObservableMatrix<bool> agentSchedule)
        {
            agentSchedule.IsObserved = false;
            int hourIdx = 0;
            foreach (XElement hourLmnt in agentLmnt.Descendants(LABEL_HOUR_NODE))
            {
                int dayIdx = 0;
                foreach (XElement dayLmnt in hourLmnt.Descendants(LABEL_DAY_NODE))
                    agentSchedule[hourIdx][dayIdx++] = bool.Parse(dayLmnt.Value);

                hourIdx++;
            }
            agentSchedule.IsObserved = true;
        }

        private void updateScheduleTime(XElement agentNode, int hourIdx, int dayIdx, bool isAvailable)
        {
            string dayStr = App.DAYS_LABELS[dayIdx];
            agentNode.Descendants(LABEL_HOUR_NODE)
                     .Where(e => (int)e.Attribute(LABEL_HOUR_NODE_HOUR_IDX_ATTRIB) == hourIdx)
                     .FirstOrDefault()
                     .Descendants(LABEL_DAY_NODE)
                     .Where(e => (string)e.Attribute(LABEL_DAY_NODE_NAME_ATTRIB) == dayStr)
                     .FirstOrDefault().SetValue(isAvailable.ToString());
        }

        private XElement findStudentLmntByName(string name)
        {
            return studentsXmlLmnt.Descendants(LABEL_STUDENT_NODE)
                                  .Where(e => (string)e.Attribute(LABEL_STUDENT_NODE_NAME_ATTRIB) == name)
                                  .FirstOrDefault();
        }

        private XElement findSchoolLmntByName(string name)
        {
            return schoolsXmlLmnt.Descendants().Where(e => e.Value.Equals(name)).FirstOrDefault();
        }
    }
}
