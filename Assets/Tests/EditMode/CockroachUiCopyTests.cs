using System.Reflection;
using IfYouWereCockroach.Prototype;
using NUnit.Framework;
using UnityEngine;

public sealed class CockroachUiCopyTests
{
    [Test]
    public void ObjectiveAndRouteCopyAreBilingual()
    {
        string objective = DemoObjectivePlanner.ObjectiveText(DemoObjectiveStep.EatFood, 1, 5, 0);
        string route = DemoObjectivePlanner.RouteHint(DemoObjectiveStep.FindHideSpot, "客厅");

        StringAssert.Contains("食物", objective);
        StringAssert.Contains("Food", objective);
        StringAssert.Contains("隐藏", route);
        StringAssert.Contains("Hide", route);
    }

    [Test]
    public void ControlCopyNamesEveryPlayableKeyInChineseAndEnglish()
    {
        MethodInfo method = typeof(DemoObjectivePlanner).GetMethod("ControlsText", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        string controls = (string)method.Invoke(null, null);
        StringAssert.Contains("移动", controls);
        StringAssert.Contains("Move", controls);
        StringAssert.Contains("WASD", controls);
        StringAssert.Contains("E", controls);
        StringAssert.Contains("Esc", controls);
    }

    [Test]
    public void SideMissionCopyCarriesCurrentObjectiveWithoutCenterBanner()
    {
        MethodInfo method = typeof(DemoObjectivePlanner).GetMethod("SideMissionText", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        string copy = (string)method.Invoke(null, new object[]
        {
            DemoObjectiveStep.LayEgg,
            2,
            5,
            false,
            1,
            1,
            true,
            1
        });

        StringAssert.Contains("当前任务", copy);
        StringAssert.Contains("Current", copy);
        StringAssert.Contains("产卵", copy);
        StringAssert.Contains("Lay egg", copy);
    }

    [Test]
    public void BuildUiPlacesEveryHudElementOnASide()
    {
        var managerObject = new GameObject("HUD layout test manager");
        var manager = managerObject.AddComponent<CockroachGameManager>();
        var buildUi = typeof(CockroachGameManager).GetMethod("BuildUi", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(buildUi);
        buildUi.Invoke(manager, null);

        var canvasObject = GameObject.Find("Prototype HUD");
        Assert.NotNull(canvasObject);

        foreach (string elementName in new[] { "Event", "Challenge Panel", "Intro Panel", "Run Result Panel" })
        {
            var element = FindChild(canvasObject.transform, elementName);
            Assert.NotNull(element, $"Missing HUD element: {elementName}");

            var rect = element.GetComponent<RectTransform>();
            Assert.NotNull(rect);
            Assert.AreNotEqual(new Vector2(0.5f, 0.5f), rect.anchorMin, $"{elementName} must not use the center of the screen");
            Assert.AreNotEqual(new Vector2(0.5f, 0f), rect.anchorMin, $"{elementName} must not use the bottom center of the screen");
        }

        Object.DestroyImmediate(canvasObject);
        Object.DestroyImmediate(managerObject);
    }

    private static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }
}
