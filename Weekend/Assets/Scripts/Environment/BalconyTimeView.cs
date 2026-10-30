// 职责：按时段切换阳台环境贴图。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DeathBySeventhsDawn.TimeSystem;
namespace DeathBySeventhsDawn.Interaction
{
    // 只管理阳台的天空、远楼、远处窗灯。不要把栏杆、地板或玩家加进远景列表。
    public sealed class BalconyTimeView : MonoBehaviour
    {
        BalconyViewSettings settings;
        readonly List<SpriteRenderer> buildings = new List<SpriteRenderer>(), windows = new List<SpriteRenderer>();
        readonly List<bool> buildingVisibility = new List<bool>(), windowVisibility = new List<bool>();
        Camera viewCamera; Color originalSky;
        SpriteRenderer photoRenderer;
        Bounds sceneryBounds;
        TimeManager clock;

        // 自动绑定现有 Balcony 场景，避免为了上色重建或改写场景文件。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Balcony")
                return;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<BalconyTimeView>() != null)
                    return;
            var controller = new GameObject("阳台外景时间变化");
            SceneManager.MoveGameObjectToScene(controller, scene);
            controller.AddComponent<BalconyTimeView>();
        }
        void Awake()
        {
            // 【配置入口】编辑 Assets/Resources/BalconyViewSettings.asset；无需修改本脚本。
            settings = Resources.Load<BalconyViewSettings>("BalconyViewSettings");
            if (settings == null)
                settings = ScriptableObject.CreateInstance<BalconyViewSettings>();
            bool hasBounds = false;
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    // 这些名字对应现有场景对象；以后若重命名远楼，请同步调整这里。
                    bool building = renderer.name.StartsWith("Distant building");
                    bool window = renderer.name == "Distant window";
                    if (!building && !window)
                        continue;
                    if (building)
                    {
                        buildings.Add(renderer);
                        buildingVisibility.Add(renderer.enabled);
                    }
                    else
                    {
                        windows.Add(renderer);
                        windowVisibility.Add(renderer.enabled);
                    }
                    if (!hasBounds)
                    {
                        sceneryBounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                        sceneryBounds.Encapsulate(renderer.bounds);
                }
                if (viewCamera == null)
                    viewCamera = root.GetComponentInChildren<Camera>();

            }

            if (viewCamera != null)
                originalSky = viewCamera.backgroundColor;
            if (!hasBounds)
                sceneryBounds = new Bounds(new Vector3(0, -3.7f, 0), new Vector3(15, 2.5f, 0));
            var photo = new GameObject("阳台外景照片（运行时）");
            photo.transform.SetParent(transform, false);
            photoRenderer = photo.AddComponent<SpriteRenderer>();
            // 使用原有不受灯光影响的材质，让照片无需额外灯光也能显示。
            if (buildings.Count > 0)
            {
                photoRenderer.sharedMaterial = buildings[0].sharedMaterial;
                photoRenderer.sortingLayerID = buildings[0].sortingLayerID;
                photoRenderer.sortingOrder = buildings[0].sortingOrder;
            }
        }
        void OnEnable()
        {
            clock = TimeManager.Instance;
            if (clock != null)
            {
                clock.TimeChanged += Refresh;
                Refresh(clock.Days, clock.Period);
            }
            else
                Refresh(1, TimePeriod.Morning);
        }
        void OnDisable()
        {
            if (clock != null)
                clock.TimeChanged -= Refresh;
            if (viewCamera != null)
                viewCamera.backgroundColor = originalSky;

        }
        void Refresh(int day, TimePeriod period)
        {
            float decay = Mathf.Clamp01((day - 1) / 6f);
            Color sky = settings.Sky(day, (int)period);
            if (viewCamera != null)
                viewCamera.backgroundColor = sky;

            Color buildingColor = Color.Lerp(settings.firstDayBuildings, settings.seventhDayBuildings, decay) * settings.Brightness((int)period);
            buildingColor.a = 1;
            Sprite photo = settings.Photo(day);
            for (int i = 0; i < buildings.Count; i++)
            {
                buildings[i].enabled = photo == null && buildingVisibility[i];
                buildings[i].color = buildingColor;
            }
            for (int i = 0; i < windows.Count; i++)
            {
                var window = windows[i];
                window.enabled = photo == null && windowVisibility[i];
                // 固定索引控制逐日熄灯，不使用随机数，重进房间也保持一致。
                bool lit = (period == TimePeriod.Evening || period == TimePeriod.Night) && i % 7 >= day - 1;
                window.color = lit ? settings.litWindows : settings.darkWindows;
            }
            photoRenderer.sprite = photo;
            photoRenderer.enabled = photo != null;
            if (photo == null)
                return;
            photoRenderer.color = settings.tintPhotosWithTime ? sky : Color.white;
            // 保持照片比例，完整放入远景区域；不会拉伸照片或覆盖阳台的行走区域。
            float size = Mathf.Max(.1f, settings.photoAreaScale);
            float factor = Mathf.Min(sceneryBounds.size.x / Mathf.Max(.01f, photo.bounds.size.x), sceneryBounds.size.y / Mathf.Max(.01f, photo.bounds.size.y)) * size;
            photoRenderer.transform.localScale = new Vector3(factor, factor, 1);
            photoRenderer.transform.position = sceneryBounds.center + (Vector3)settings.photoOffset - photo.bounds.center * factor;
        }
    }
}
