using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TeacherScheduler
{
    // TODO: fix this to enable addition and removal of columns and rows
    public class ObservableMatrix<T> : BindingList<ObservableCollection<T>>
    {
        private int changedRowIdx;        

        public delegate void ObservableMatrixChangedEventHandler(int changedRowIdx, int changedColIdx, T newVal);
        public event ObservableMatrixChangedEventHandler MatrixChanged;

        public bool IsObserved { get; set; } 

        public ObservableMatrix(int initRowsNr, int initColsNr) : base(constructMatrix(initRowsNr, initColsNr)) {
            foreach (ObservableCollection<T> row in this)
                row.CollectionChanged += onRowUpdated;
            ListChanged += onColUpdated;
            IsObserved = true;
        }

        private static ObservableCollection<ObservableCollection<T>> constructMatrix(int rowsNr, int colsNr)
        {
            List<ObservableCollection<T>> matBuffer = new List<ObservableCollection<T>>(rowsNr);
            for (int rowIdx = 0; rowIdx < rowsNr; rowIdx++)            
                matBuffer.Add(new ObservableCollection<T>(Enumerable.Repeat(default(T), colsNr)));
            
            return new ObservableCollection<ObservableCollection<T>>(matBuffer);
        }

        private void onRowUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            int changedColIdx = e.NewStartingIndex;
            if (IsObserved)
                MatrixChanged?.Invoke(changedRowIdx, changedColIdx, this[changedRowIdx][changedColIdx]);
        }

        protected void onColUpdated(object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.ItemAdded:
                    break;
                case ListChangedType.ItemDeleted:
                    break;
                case ListChangedType.ItemChanged:
                    changedRowIdx = e.NewIndex;
                    break;
            }            
        }
    }
}
