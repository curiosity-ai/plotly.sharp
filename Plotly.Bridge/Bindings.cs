﻿namespace PlotlyBridge
{
    using System;
    using Types;
    using System.Collections.Generic;
    using global::Bridge;
    using static Retyped.dom;
    using System.Linq;
    using Retyped;
    using System.ComponentModel.DataAnnotations.Schema;
    using static Retyped.es5;

    public interface IPlot
    {
        HTMLElement Render();
    }

    [ObjectLiteral(ObjectCreateMode.Constructor)]
    public class Box<T>
    {
        public Box(string key, object value)
        {
            Script.Write("{0}[{1}] = {2}", this, key, value);
        }
    }

    public static class Bindings
    {

        public static object flatten2DArrayIf1D<T>(IEnumerable<IEnumerable<T>> values)
        {
            return values.Count() == 1 ? (object)(values.First().ToArray()) : values.Select(a => a.ToArray()).ToArray();
        }
        public static object flattenProperties<T>(IEnumerable<Box<T>> properties)
        {
            object result = new object();
            foreach (var prop in properties)
            {
                Script.Write("result = Object.assign(result, {0})", prop);
            }
            return result;
        }
        public static IPlot createPlot(params Box<IPlotProperty>[] props) 
        {
            CheckForPlotlyOrLoadFromCDN();
            return new PlotlyPlot(props);
        }

        private static void CheckForPlotlyOrLoadFromCDN()
        {
            //TODO
        }

        public static Box<IPlotProperty> extractTraces(IEnumerable<Box<ITracesProperty>> props)
        {
            return Interop.mkPlotAttr("data", flattenPropertiesToArray(props));
        }

        public static object[] flattenPropertiesToArray<T>(IEnumerable<Box<T>> props)
        {
            var all = props.ToArray();

            object[] results = new object[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                var p = all[i];
                var keys = object.GetOwnPropertyNames(p);

                foreach (var k in keys)
                {
                    if (k.StartsWith("$")) continue;
                    results[i] = p[k];
                    break;
                }
            }

            return results;
        }

        public static Box<IPlotProperty> extractConfig(IEnumerable<Box<IConfigProperty>> props)
        {
            return Interop.mkPlotAttr("config", flattenProperties(props));
        }

        public static Box<IPlotProperty> extractLayout(IEnumerable<Box<ILayoutProperty>> props)
        {
            return Interop.mkPlotAttr("layout", flattenProperties(props));
        }

        private class PlotlyPlot :IPlot
        {
            public PlotlyPlot(IEnumerable<Box<IPlotProperty>> props)
            {
                Props = flattenProperties(props);
            }

            public object Props { get; }

            private bool IsRendered;
            private HTMLElement Container;

            public HTMLElement Render()
            {
                Container = Container ?? new HTMLDivElement();

                object data = Props["data"] ?? new object();
                object layout = Props["layout"] ?? new object();
                object config = Props["config"] ?? new object();


                data = JSON.parse(JSON.stringify(data));
                layout = JSON.parse(JSON.stringify(layout));
                config = JSON.parse(JSON.stringify(config));

                console.log(data);
                console.log(layout);
                console.log(config);

                if (IsRendered)
                {
                    Script.Write("Plotly.react({0}, {1}, {2}, {3})",
                                 Container,
                                 data,
                                 layout,
                                 config);
                }
                else
                {
                    Script.Write("Plotly.newPlot({0}, {1}, {2}, {3})",
                                 Container,
                                 data,
                                 layout,
                                 config);
                }

                //onError onPurge onUpdate  onInitialized useResizeHandler events should go to the newPlot

                BindPlotlyEvents(Props, Container);

                IsRendered = true;
                return Container;
            }

            private static void BindPlotlyEvents(object props, HTMLElement container)
            {
                foreach(var e in GetOwnPropertyNames(props))
                {
                    BindIfAny(e, container, Script.Write<Action<object>>("props[e]"));
                }
            }

            private static void BindIfAny(string name, HTMLElement container, Action<object> action)
            {
                if(action == null) { return; }

                string plotlyEventName;

                switch (name)
                {
                    case "onClick"                :  { plotlyEventName = "plotly_click"; break; }
                    case "onAfterPlot"            :  { plotlyEventName = "plotly_afterplot"; break; }
                    case "onAnimated"             :  { plotlyEventName = "plotly_animated"; break; }
                    case "onAnimatingFrame"       :  { plotlyEventName = "plotly_animatingframe"; break; }
                    case "onAnimationInterrupted" :  { plotlyEventName = "plotly_animationinterrupted"; break; }
                    case "onAutoSize"             :  { plotlyEventName = "plotly_autosize"; break; }
                    case "onBeforeExport"         :  { plotlyEventName = "plotly_beforeexport"; break; }
                    case "onButtonClicked"        :  { plotlyEventName = "plotly_buttonclicked"; break; }
                    case "onClickAnnotation"      :  { plotlyEventName = "plotly_clickannotation"; break; }
                    case "onDeselect"             :  { plotlyEventName = "plotly_deselect"; break; }
                    case "onDoubleClick"          :  { plotlyEventName = "plotly_doubleclick"; break; }
                    case "onFramework"            :  { plotlyEventName = "plotly_framework"; break; }
                    case "onHover"                :  { plotlyEventName = "plotly_hover"; break; }
                    case "onLegendClick"          :  { plotlyEventName = "plotly_legendclick"; break; }
                    case "onLegendDoubleClick"    :  { plotlyEventName = "plotly_legenddoubleclick"; break; }
                    case "onRelayout"             :  { plotlyEventName = "plotly_relayout"; break; }
                    case "onRestyle"              :  { plotlyEventName = "plotly_restyle"; break; }
                    case "onRedraw"               :  { plotlyEventName = "plotly_redraw"; break; }
                    case "onSelected"             :  { plotlyEventName = "plotly_selected"; break; }
                    case "onSelecting"            :  { plotlyEventName = "plotly_selecting"; break; }
                    case "onSliderChange"         :  { plotlyEventName = "plotly_sliderchange"; break; }
                    case "onSliderEnd"            :  { plotlyEventName = "plotly_sliderend"; break; }
                    case "onSliderStart"          :  { plotlyEventName = "plotly_sliderstart"; break; }
                    case "onTransitioning"        :  { plotlyEventName = "plotly_transitioning"; break; }
                    case "onTransitionInterrupted":  { plotlyEventName = "plotly_transitioninterrupted"; break; }
                    case "onUnhover"              :  { plotlyEventName = "plotly_unhover"; break; }
                    default:return;
                }

                Script.Write("{0}.on({1}, {2})", container, plotlyEventName, action);
            }
        }
    }
}