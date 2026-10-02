using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using IPA.Utilities;
using IPA.Utilities.Async;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using TakeMeToResults.AffinityPatches;
using UnityEngine;
using Zenject;

namespace TakeMeToResults.UI
{
    internal class ResultsButtonController : IInitializable, IDisposable, INotifyPropertyChanged
    {
        private readonly TitleViewController titleViewController;
        private readonly ResultsViewController resultsViewController;
        private readonly MainFlowCoordinator mainFlowCoordinator;
        private readonly PresentFlowCoordinatorPatch presentFlowCoordinatorPatch;

        private ViewController leftScreenViewController;
        private ViewController rightScreenViewController;
        private ViewController bottomScreenViewController;
        private ViewController topScreenViewController;
        private ViewController mainScreenViewController;
        private FlowCoordinator deepestChildFlowCoordinator;

        [Inject]
        private LevelCollectionNavigationController levelCollectionNavigationController = null;

        private SignalOnUIButtonClick SignalOnUIButtonClick;

        public event PropertyChangedEventHandler PropertyChanged;
        private readonly Action ShowOther;
        private bool disposed;

        [UIComponent("results-button")]
        private RectTransform resultsButtonTransform { get; set; }

        public ResultsButtonController(HierarchyManager hierarchyManager, ResultsViewController resultsViewController, MainFlowCoordinator mainFlowCoordinator,
            PresentFlowCoordinatorPatch presentFlowCoordinatorPatch)
        {
            ScreenSystem screenSystem = hierarchyManager.GetField<ScreenSystem, HierarchyManager>("_screenSystem");
            titleViewController = screenSystem.titleViewController;
            this.resultsViewController = resultsViewController;
            this.mainFlowCoordinator = mainFlowCoordinator;
            this.presentFlowCoordinatorPatch = presentFlowCoordinatorPatch;
            ShowOther = ShowOtherViewControllers;
            SignalOnUIButtonClick = titleViewController.transform.Find("BackButton").GetComponent<SignalOnUIButtonClick>();
        }

        public void Initialize()
        {
            if (disposed) return;
            resultsViewController.continueButtonPressedEvent += GetViewControllers;
            levelCollectionNavigationController.didActivateEvent += DidActivate;
            levelCollectionNavigationController.didChangeLevelDetailContentEvent += UpdateContent;
            SignalOnUIButtonClick._buttonClickedSignal.Subscribe(OnBackButtonPressed);

            Task<string> markup = Plugin.ResultsButtonMarkupTask;
            if (markup.IsCompleted)
            {
                CreateResultsButton(markup);
            }
            else
            {
                _ = markup.ContinueWith(CreateResultsButton, CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, UnityMainThreadTaskScheduler.Default);
            }
        }

        private void CreateResultsButton(Task<string> markup)
        {
            if (disposed || titleViewController == null || resultsViewController == null || levelCollectionNavigationController == null)
                return;

            try
            {
                BSMLParser.Instance.Parse(markup.GetAwaiter().GetResult(), titleViewController.gameObject, this);
                resultsButtonTransform.gameObject.name = "TakeMeToResults";
            }
            catch (Exception exception)
            {
                Plugin.Log.Error($"Unable to initialize results button: {exception}");
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            resultsViewController.continueButtonPressedEvent -= GetViewControllers;
            levelCollectionNavigationController.didActivateEvent -= DidActivate;
            levelCollectionNavigationController.didChangeLevelDetailContentEvent -= UpdateContent;
            SignalOnUIButtonClick._buttonClickedSignal.Unsubscribe(OnBackButtonPressed);
        }

        private void GetViewControllers(ResultsViewController resultsViewController)
        {
            deepestChildFlowCoordinator = mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();

            leftScreenViewController = deepestChildFlowCoordinator.GetField<ViewController, FlowCoordinator>("_leftScreenViewController");
            rightScreenViewController = deepestChildFlowCoordinator.GetField<ViewController, FlowCoordinator>("_rightScreenViewController");
            bottomScreenViewController = deepestChildFlowCoordinator.GetField<ViewController, FlowCoordinator>("_bottomScreenViewController");
            topScreenViewController = deepestChildFlowCoordinator.GetField<ViewController, FlowCoordinator>("_topScreenViewController");
            mainScreenViewController = deepestChildFlowCoordinator.topViewController;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonActive)));
        }

        private void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonActive)));
        }

        private void UpdateContent(LevelCollectionNavigationController a, StandardLevelDetailViewController.ContentType contentType)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonActive)));
        }

        private void OnBackButtonPressed()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonActive)));
        }

        [UIAction("results-click")]
        private void ResultsClick()
        {
            if (!(deepestChildFlowCoordinator is SinglePlayerLevelSelectionFlowCoordinator))
            {
                return;
            }

            if (mainScreenViewController != null)
            {
                deepestChildFlowCoordinator.InvokeMethod<object, FlowCoordinator>("PresentViewController", new object[] { mainScreenViewController, ShowOther, ViewController.AnimationDirection.Vertical, false });
            }
        }

        private void ShowOtherViewControllers()
        {
            if (leftScreenViewController != null)
            {
                deepestChildFlowCoordinator.InvokeMethod<object, FlowCoordinator>("SetLeftScreenViewController", new object[] { leftScreenViewController, ViewController.AnimationType.In });
            }

            if (rightScreenViewController != null)
            {
                deepestChildFlowCoordinator.InvokeMethod<object, FlowCoordinator>("SetRightScreenViewController", new object[] { rightScreenViewController, ViewController.AnimationType.In });
            }

            if (bottomScreenViewController != null)
            {
                deepestChildFlowCoordinator.InvokeMethod<object, FlowCoordinator>("SetBottomScreenViewController", new object[] { bottomScreenViewController, ViewController.AnimationType.In });
            }

            if (topScreenViewController != null)
            {
                deepestChildFlowCoordinator.InvokeMethod<object, FlowCoordinator>("SetTopScreenViewController", new object[] { topScreenViewController, ViewController.AnimationType.In });
            }
        }

        [UIValue("button-active")]
        private bool ButtonActive => levelCollectionNavigationController != null && levelCollectionNavigationController.isActiveAndEnabled && resultsViewController._levelCompletionResults != null;
    }
}
