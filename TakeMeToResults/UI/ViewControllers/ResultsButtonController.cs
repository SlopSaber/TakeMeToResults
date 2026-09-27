using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using IPA.Utilities;
using System;
using System.ComponentModel;
using System.Reflection;
using TakeMeToResults.AffinityPatches;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace TakeMeToResults.UI
{
    internal class ResultsButtonController : IInitializable, IDisposable, INotifyPropertyChanged
    {
        private const float TitleUnderlineHeightScale = 1.6f;

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
            BSMLParser.Instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "TakeMeToResults.UI.Views.ResultsButton.bsml"), titleViewController.gameObject, this);
            resultsButtonTransform.gameObject.name = "TakeMeToResults";
            var underline = resultsButtonTransform.Find("Underline");
            if (underline != null)
            {
                var effect = underline.gameObject.AddComponent<ResultsTitleUnderlineHeightEffect>();
                effect.HeightScale = TitleUnderlineHeightScale;
            }
            resultsViewController.continueButtonPressedEvent += GetViewControllers;
            levelCollectionNavigationController.didActivateEvent += DidActivate;
            levelCollectionNavigationController.didChangeLevelDetailContentEvent += UpdateContent;
            SignalOnUIButtonClick._buttonClickedSignal.Subscribe(OnBackButtonPressed);
        }

        public void Dispose()
        {
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

    internal sealed class ResultsTitleUnderlineHeightEffect : BaseMeshEffect
    {
        public float HeightScale { get; set; } = 1f;

        public override void ModifyMesh(VertexHelper vertices)
        {
            if (!IsActive()) return;
            float bottom = graphic.rectTransform.rect.yMin;
            UIVertex vertex = default;
            for (int i = 0; i < vertices.currentVertCount; i++)
            {
                vertices.PopulateUIVertex(ref vertex, i);
                var position = vertex.position;
                position.y = bottom + (position.y - bottom) * HeightScale;
                vertex.position = position;
                vertices.SetUIVertex(vertex, i);
            }
        }
    }
}
