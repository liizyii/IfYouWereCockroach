using System.Reflection;
using IfYouWereCockroach.Prototype;
using NUnit.Framework;

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
}
