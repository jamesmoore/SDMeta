﻿using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components;
using SDMetaUI.Models;
using SDMetaUI.Shared;
using SDMeta.Cache;

namespace SDMetaUI.Pages
{
    public partial class Index
    {
        [Parameter]
        [SupplyParameterFromQuery(Name = "filter")]
        public string? Filter { get; set; }

        private string FilterInput { get; set; }

        private FullScreenView? fullScreenView;

        Action<ChangeEventArgs> onInputDebounced;

        IList<ModelSummaryViewModel> modelsList;
        private int Added => this.FileSystemObserver.AddedCount;
        private int Removed => this.FileSystemObserver.RemovedCount;

        public string PageTitle => "Gallery" + (string.IsNullOrWhiteSpace(this.Filter) ? "" : " - " + this.Filter);

        /// <summary>
        /// For when the back/forward navigation is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LocationChanged(object sender, LocationChangedEventArgs e)
        {
            if (this.viewModel.Filter != this.Filter)
            {
                this.viewModel.Filter = this.Filter;
                this.FilterInput = this.Filter;
                this.StateHasChanged();
            }
        }

        protected override void OnInitialized()
        {
            this.modelsList = viewModel.GetModelsList();
            NavigationManager.LocationChanged += LocationChanged;
            FileSystemObserver.FileSystemChanged += OnFileSystemChanged;
            this.rescan.ProgressNotification += ProgressNotification;
        }

        private float scanProgess = 0;

        private void ProgressNotification(object sender, float i)
        {
            this.InvokeAsync(() =>
            {
                scanProgess = i;
                this.StateHasChanged();
            });
        }

        private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
        {
            this.InvokeAsync(() =>
            {
                this.StateHasChanged();
            });
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                onInputDebounced = Debounce<ChangeEventArgs>(e =>
                {
                    this.InvokeAsync(() =>
                    {
                        var updatedFilter = (string)e.Value;
                        if (updatedFilter == string.Empty) updatedFilter = null;
                        viewModel.Filter = updatedFilter;
                        NavigationManager.NavigateTo(NavigationManager.GetUriWithQueryParameter("filter", updatedFilter));
                        this.StateHasChanged();
                    });
                }, TimeSpan.FromMilliseconds(500));

                resizeListener.OnResized += (sender, size) =>
                {
                    viewModel.Width = size.Width;
                    this.StateHasChanged();
                };

                if (viewModel.Width == 0)
                {
                    viewModel.Width = (await resizeListener.GetBrowserWindowSize()).Width;
                }
                LoadData();
                // Update filter from initial URL
                this.viewModel.Filter = this.Filter;
                this.FilterInput = this.Filter;
                this.StateHasChanged();
            }

            if (viewModel.AutoRescan && (this.FileSystemObserver.AddedCount > 0 || this.FileSystemObserver.RemovedCount > 0))
            {
                await this.PartialRescan();
                this.StateHasChanged();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private void LoadData()
        {
            viewModel.Initialize();
        }

        void OnSelectModel(ChangeEventArgs e)
        {
            var selectedModelId = int.Parse(e.Value.ToString());
            var model = modelsList.FirstOrDefault(p => p.Id == selectedModelId);
            this.viewModel.ModelFilter = model.Id == 0 ? null : model;
        }

        private void imageClickParent(PngFileViewModel model)
        {
            this.viewModel.ToggleExpandedState(model);
        }

        private async void imageClick(PngFileViewModel model)
        {
            if (model == viewModel.SelectedFile)
            {
                if (fullScreenView.IsOpen == false)
                {
                    await fullScreenView.ShowAsync();
                }
            }
            else
            {
                this.viewModel.SelectedFile = model;
            }
        }

        private async Task FullRescan()
        {
            this.FileSystemObserver.Reset();
            await this.rescan.ProcessPngFiles();
            logger.LogInformation("Rescan done");
            this.scanProgess = 0;
            LoadData();
        }

        private async Task PartialRescan()
        {
            await this.rescan.PartialRescan(FileSystemObserver.DequeueAdded(), FileSystemObserver.DequeueRemoved());
            logger.LogInformation("Partial Rescan done");
            this.scanProgess = 0;
            LoadData();
        }

        public void Dispose()
        {
            // Unsubscribe from the event when our component is disposed
            NavigationManager.LocationChanged -= LocationChanged;
            this.FileSystemObserver.FileSystemChanged -= this.OnFileSystemChanged;
        }

        private async void imageDelete()
        {
            if (this.viewModel.SelectedFile != null)
            {
                try
                {
                    var filename = this.viewModel.SelectedFile.FileName;
                    this.FileSystemObserver.RegisterRemoval(filename);
                    this.viewModel.RemoveFile();
                    this.StateHasChanged();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete.");
                    Messenger.AddError(title: "Delete", message: "Delete failed");
                }
            }
        }

        private async void leftKey()
        {
            viewModel.MovePrevious();
        }

        private async void rightKey()
        {
            viewModel.MoveNext();
        }

        // https://www.meziantou.net/debouncing-throttling-javascript-events-in-a-blazor-application.htm
        Action<T> Debounce<T>(Action<T> action, TimeSpan interval)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            var last = 0;
            return arg =>
            {
                var current = System.Threading.Interlocked.Increment(ref last);
                Task.Delay(interval).ContinueWith(task =>
                {
                    if (current == last)
                    {
                        action(arg);
                    }
                });
            };
        }
        private void OnSelectSortBy(ChangeEventArgs e)
        {
            var newOrder = Enum.Parse<QuerySortBy>(e.Value.ToString());
            viewModel.SortBy = newOrder;
        }
    }
}
