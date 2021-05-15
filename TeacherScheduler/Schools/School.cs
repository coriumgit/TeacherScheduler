using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeacherScheduler
{
    public class School : ObservableObject, System.IEquatable<School>
    {
        private string name = "school";
        public string Name
        {
            get 
            {
                return name;
            }
            set
            {
                if (name != value)
                {                    
                    DataBaseManager.Instance.updateSchoolName(name, value);
                    name = value;

                    OnPropertyChanged("Name");
                }
            }
        }

        public School() {}
        
        public School(string name)
        {
            this.name = name;
        }

        public override bool Equals(object other)
        {
            return Equals(other as School);
        }

        public bool Equals(School other)
        {
            if (ReferenceEquals(other, null))
                return false;
            else if (ReferenceEquals(other, this))
                return true;
            else if (GetType() != other.GetType())
                return false;
            else
                return Name.Equals(other.Name);
        }

        public static bool operator==(School lhs, School rhs)
        {
            if (ReferenceEquals(lhs, null))
            {
                if (ReferenceEquals(rhs, null))               
                    return true;
                else                
                    return false;
            }

            return lhs.Equals(rhs);
        }

        public static bool operator !=(School lhs, School rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
