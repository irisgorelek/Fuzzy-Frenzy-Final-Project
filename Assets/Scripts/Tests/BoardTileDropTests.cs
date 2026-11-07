using System.Linq;
using NUnit.Framework;
using UnityEngine;

// The black-sheep fall/spawn reporting this file used to test now lives in BoardRulesTests.
public class BoardTileDropTests
{
    [Test]
    public void Drop_ReachesGroundThenReboundsAndSettlesExactly()
    {
        var go = new GameObject("Drop test", typeof(RectTransform));
        object tween = null;
        try
        {
            var rect = go.GetComponent<RectTransform>();
            rect.position = new Vector3(0,100,0);
            var method = typeof(BoardTileDropAnimation).GetMethod("Create");
            tween = method.Invoke(null, new object[] { rect, Vector3.zero, Vector3.up * 8, .2f, .14f });
            var extensions = tween.GetType().Assembly.GetType("DG.Tweening.TweenExtensions");
            var goTo = extensions.GetMethods().Single(m => m.Name == "Goto" && !m.IsGenericMethod && m.GetParameters().Length == 3);
            goTo.Invoke(null, new[] { tween, (object).2f, false });
            Assert.Less(rect.position.magnitude, .001f);
            goTo.Invoke(null, new[] { tween, (object).263f, false });
            Assert.That(rect.position.y, Is.EqualTo(8).Within(.001f));
            goTo.Invoke(null, new[] { tween, (object).34f, false });
            Assert.Less(rect.position.magnitude, .001f);
        }
        finally
        {
            if (tween != null)
                tween.GetType().Assembly.GetType("DG.Tweening.TweenExtensions").GetMethods()
                    .Single(m => m.Name == "Kill" && !m.IsGenericMethod && m.GetParameters().Length == 2)
                    .Invoke(null, new[] { tween, (object)false });
            Object.DestroyImmediate(go);
        }
    }
}
