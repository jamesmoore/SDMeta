using SDMeta.Cache;
using System.Collections;

namespace SDMetaUI.Models
{
    public class FilteredList(
        IPngFileDataSource pngFileDataSource,
        PngFileViewModelBuilder pngFileViewModelBuilder,
        Action postFilteringAction
            ) : IList<PngFileViewModel>
    {
        private IList<PngFileViewModel> filteredFiles = new List<PngFileViewModel>();

        public bool FilterError { get; private set; }

        private ModelSummaryViewModel modelFilter;
        public ModelSummaryViewModel ModelFilter
        {
            get
            {
                return modelFilter;
            }
            set
            {
                if (modelFilter != value)
                {
                    modelFilter = value;
                    RunFilter();
                }
            }
        }

        private string filter;

        public string Filter
        {
            get
            {
                return filter;
            }
            set
            {
                if (filter != value)
                {
                    filter = value;
                    RunFilter();
                }
            }
        }

        private QuerySortBy sortBy = QuerySortBy.Newest;

        public QuerySortBy SortBy
        {
            get
            {
                return sortBy;
            }
            set
            {
                if (sortBy != value)
                {
                    sortBy = value;
                    RunFilter();
                }
            }
        }

        public int Count => this.filteredFiles.Count;

        public bool IsReadOnly => throw new NotImplementedException();

        public PngFileViewModel this[int index]
        {
            get => this.filteredFiles[index];
            set => throw new NotImplementedException();
        }

        public void RunFilter()
        {
            var queryParams = new QueryParams(
                this.filter,
                this.modelFilter == null ? null : new ModelFilter(this.modelFilter.Model,this.modelFilter.ModelHash),
                sortBy
            );
            try
            {
                filteredFiles = pngFileDataSource.Query(queryParams).Select(p => pngFileViewModelBuilder.BuildModel(p)).ToList();
                this.FilterError = false;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("fts5"))
                {
                    this.FilterError = true;
                }
                else
                {
                    throw;
                }
            }
            postFilteringAction();
        }

        public PngFileViewModel? Get(string path)
        {
            return filteredFiles.FirstOrDefault(p => p.FileName == path);
        }

        public int IndexOf(PngFileViewModel item)
        {
            return filteredFiles.IndexOf(item);
        }

        public void Insert(int index, PngFileViewModel item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(PngFileViewModel item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(PngFileViewModel item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(PngFileViewModel[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(PngFileViewModel item)
        {
            return this.filteredFiles.Remove(item);
        }

        public IEnumerator<PngFileViewModel> GetEnumerator()
        {
            return this.filteredFiles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.filteredFiles.GetEnumerator();
        }
    }
}
