using SDMeta.Cache;
using System.Collections;

namespace SDMetaUI.Models
{
    public class FilteredList(
        IImageFileDataSource imageFileDataSource,
        ImageFileViewModelBuilder imageFileViewModelBuilder,
        Action postFilteringAction
            ) : IList<ImageFileViewModel>
    {
        private IList<ImageFileViewModel> filteredFiles = new List<ImageFileViewModel>();

        public bool FilterError { get; private set; }

        private ModelSummaryViewModel modelFilter = ModelSummaryViewModel.AllModels;
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

        private string? filter;

        public string? Filter
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

        public ImageFileViewModel this[int index]
        {
            get => this.filteredFiles[index];
            set => throw new NotImplementedException();
        }

        public void RunFilter()
        {
            var queryParams = new QueryParams(
                this.filter,
                this.modelFilter == ModelSummaryViewModel.AllModels ? null : new ModelFilter(this.modelFilter.Model, this.modelFilter.ModelHash),
                sortBy
            );
            try
            {
                filteredFiles = imageFileDataSource.Query(queryParams).Select(p => imageFileViewModelBuilder.BuildModel(p)).ToList();
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

        public ImageFileViewModel? Get(string path)
        {
            return filteredFiles.FirstOrDefault(p => p.FileName == path);
        }

        public int IndexOf(ImageFileViewModel item)
        {
            return filteredFiles.IndexOf(item);
        }

        public void Insert(int index, ImageFileViewModel item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(ImageFileViewModel item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(ImageFileViewModel item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(ImageFileViewModel[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(ImageFileViewModel item)
        {
            return this.filteredFiles.Remove(item);
        }

        public IEnumerator<ImageFileViewModel> GetEnumerator()
        {
            return this.filteredFiles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.filteredFiles.GetEnumerator();
        }
    }
}
