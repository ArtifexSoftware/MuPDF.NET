using System.Data;
using System.Net;
using System.Reflection;
using System.Text;
using static MuPDF.NET.Global;

namespace MuPDF.NET
{
    public class Global
    {
        public class Edge
        {
            public float x0;
            public float y0;
            public float x1;
            public float y1;
            public float width;
            public float height;
            public Point[] pts;
            public float linewidth; 
            public bool stroke;
            public bool fill;
            public bool evenodd;
            public float[] stroking_color;
            public float[] non_stroking_color;
            public string object_type;
            public int page_number;
            public object stroking_pattern;
            public object non_stroking_pattern;
            public float top;
            public float bottom;
            public float doctop;
            public string orientation;
        }

        public class Character
        {
            public float adv;
            public float bottom;
            public float doctop;
            public string fontname;
            public float height;
            public Matrix matrix;
            public string ncs;
            public int non_stroking_color;
            public object non_stroking_pattern;
            public string object_type;
            public int page_number;
            public float size;
            public int stroking_color;
            public object stroking_pattern;
            public string text;
            public float top;
            public bool upright;
            public int direction;
            public int rotation;
            public float width;
            public float x0;
            public float x1;
            public float y0;
            public float y1;
        }

        // Function to check if the extracted text contains only whitespace characters
        public static bool whiteSpaces_issuperset(string text)
        {
            HashSet<char> whiteSpaces = new HashSet<char>(new[] {
                ' ', '\t', '\n', '\r', '\v', '\f'
            });
            // Check if all characters in the extracted text are whitespace characters
            return text.All(c => whiteSpaces.Contains(c));
        }

        public class BBox
        {
            public float x0 { get; set; }
            public float top { get; set; }
            public float x1 { get; set; }
            public float bottom { get; set; }

            public BBox(float x0, float top, float x1, float bottom)
            {
                this.x0 = x0;
                this.top = top;
                this.x1 = x1;
                this.bottom = bottom;
            }

            // Union method: Combine two rectangles into one that covers both.
            public BBox Union(BBox other)
            {
                float newX0 = Math.Min(this.x0, other.x0);
                float newTop = Math.Min(this.top, other.top);
                float newX1 = Math.Max(this.x1, other.x1);
                float newBottom = Math.Max(this.bottom, other.bottom);

                return new BBox(newX0, newTop, newX1, newBottom);
            }

            // Overload the |= operator to union two rectangles.
            public static BBox operator |(BBox r1, BBox r2)
            {
                return r1.Union(r2);
            }

            public bool IsEmpty()
            {
                if (x0 == 0 && top == 0 && x1 == 0 && bottom == 0)
                    return true;
                return false;
            }

            // Override Equals and GetHashCode for Distinct to work correctly
            public override bool Equals(object obj)
            {
                return obj is BBox bbox &&
                       x0 == bbox.x0 &&
                       top == bbox.top &&
                       x1 == bbox.x1 &&
                       bottom == bbox.bottom;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(x0, top, x1, bottom);
            }

            public static BBox RectToBBox(Rect rect)
            {
                return new BBox(rect.X0, rect.Y0, rect.X1, rect.Y1);
            }

            public static Rect BBoxToRect(BBox bbox)
            {
                return new Rect(bbox.x0, bbox.top, bbox.x1, bbox.bottom);
            }
        }

        public static Edge line_to_edge(Edge line)
        {
            // Create a new dictionary to hold the edge data
            var edge = line;

            // Determine the orientation
            string orientation = (Convert.ToSingle(line.top) == Convert.ToSingle(line.bottom)) ? "h" : "v";

            // Add or update the "orientation" key in the dictionary
            edge.orientation = orientation;

            return edge;
        }

        public static List<Edge> rect_to_edges(Edge rect)
        {
            Edge top = new Edge
            {
                object_type = "rect_edge",
                height = 0,
                y0 = rect.y1,
                bottom = rect.top,
                orientation = "h"
            };

            Edge bottom = new Edge
            {
                object_type = "rect_edge",
                height = 0,
                y1 = rect.y0,
                top = rect.top + rect.height,
                doctop = rect.doctop + rect.height,
                orientation = "h"
            };

            Edge left = new Edge
            {
                object_type = "rect_edge",
                width = 0,
                x1 = rect.x0,
                orientation = "v"
            };

            Edge right = new Edge
            {
                object_type = "rect_edge",
                width = 0,
                x0 = rect.x1,
                orientation = "v"
            };

            return new List<Edge> { top, bottom, left, right };
        }

        public static List<Edge> curve_to_edges(Edge curve)
        {
            // Extract points and other properties from the curve
            Point[] points = curve.pts;

            var edges = new List<Edge>();

            for (int i = 0; i < points.Length - 1; i++)
            {
                Point p0 = points[i];
                Point p1 = points[i + 1];

                var edge = new Edge
                {
                    object_type = "curve_edge",
                    x0 = Math.Min(p0.X, p1.X),
                    x1 = Math.Max(p0.X, p1.X),
                    top = Math.Min(p0.Y, p1.Y),
                    doctop = Math.Min(p0.Y, p1.Y) + (curve.doctop - curve.top),
                    bottom = Math.Max(p0.Y, p1.Y),
                    width = Math.Abs(p0.X - p1.X),
                    height = Math.Abs(p0.Y - p1.Y),
                    orientation = (p0.X == p1.X) ? "v" : (p0.Y == p1.Y) ? "h" : null
                };

                edges.Add(edge);
            }

            return edges;
        }

        public static List<Edge> obj_to_edges(Edge obj)
        {
            string type = obj.object_type;

            if (type.Contains("_edge"))
            {
                // If it's an edge object, return it as-is.
                return new List<Edge> { obj };
            }
            else if (type == "line")
            {
                // If it's a line, process it using line_to_edge (you'll need to define line_to_edge method)
                return new List<Edge> { line_to_edge(obj) };
            }
            else if (type == "rect")
            {
                return rect_to_edges(obj);
            }
            else if (type == "curve")
            {
                return curve_to_edges(obj);
            }
            return null;
        }

        // Filter edges based on orientation, type, and minimum length
        public static List<Edge> filter_edges(
            List<Edge> edges,
            string orientation = null,
            string edgeType = null,
            float minLength = 1
        )
        {
            // Validate orientation
            if (orientation != null && orientation != "v" && orientation != "h")
            {
                throw new ArgumentException("Orientation must be 'v' or 'h'");
            }

            // Function to test if an edge meets the criteria
            bool test(Edge e)
            {
                // Determine the dimension (width or height) based on orientation
                float dimension = (e.orientation == "v") ? e.height : e.width;

                bool etCorrect = edgeType == null || e.object_type == edgeType;
                bool orientCorrect = orientation == null || e.orientation == orientation;

                return etCorrect && orientCorrect && dimension >= minLength;
            }

            // Use LINQ to filter edges
            return edges.Where(test).ToList();
        }

        public static List<List<float>> cluster_list(List<float> xs, float tolerance = 0f)
        {
            if (tolerance == 0)
            {
                return xs.OrderBy(x => x).Select(x => new List<float> { x }).ToList();
            }

            if (xs.Count < 2)
            {
                return xs.OrderBy(x => x).Select(x => new List<float> { x }).ToList();
            }

            var groups = new List<List<float>>();
            xs.Sort();
            var currentGroup = new List<float> { xs[0] };
            float last = xs[0];

            foreach (var x in xs.Skip(1))
            {
                if (x <= last + tolerance)
                {
                    currentGroup.Add(x);
                }
                else
                {
                    groups.Add(new List<float>(currentGroup));
                    currentGroup = new List<float> { x };
                }
                last = x;
            }

            groups.Add(currentGroup);
            return groups;
        }

        public static Dictionary<float, int> make_cluster_dict(List<float> values, float tolerance)
        {
            var clusters = cluster_list(values.Distinct().ToList(), tolerance);
            var clusterDict = new Dictionary<float, int>();

            var index = 0;
            foreach (var cluster in clusters)
            {
                foreach (var value in cluster)
                {
                    clusterDict[value] = index;
                }
                index++;
            }

            return clusterDict;
        }

        public static List<List<T>> cluster_objects<T>(List<T> xs, Func<T, float> keyFn, float tolerance)
        {
            var values = xs.Select(keyFn).ToList();
            var clusterDict = make_cluster_dict(values, tolerance);

            var clusterTuples = xs.Select(x => new { Object = x, ClusterId = clusterDict[keyFn(x)] })
                                  .OrderBy(t => t.ClusterId)
                                  .ToList();

            var grouped = clusterTuples.GroupBy(t => t.ClusterId)
                                       .Select(g => g.Select(t => t.Object).ToList())
                                       .ToList();
            return grouped;
        }

        public static Edge move_object(Edge obj, string axis, float value)
        {
            // Ensure the axis is valid
            if (axis != "h" && axis != "v")
            {
                throw new ArgumentException("Axis must be 'h' or 'v'", nameof(axis));
            }

            // Prepare the new property values
            var newProperties = new List<(string, float)>();

            if (axis == "h")
            {
                newProperties.Add(("x0", obj.x0 + value));
                newProperties.Add(("x1", obj.x1 + value));
            }

            if (axis == "v")
            {
                newProperties.Add(("top", obj.top + value));
                newProperties.Add(("bottom", obj.bottom + value));

                // Handle optional properties if they exist
                if (obj.doctop >= 0f)
                {
                    newProperties.Add(("doctop", obj.doctop + value));
                }

                if (obj.y0 >= 0f)
                {
                    newProperties.Add(("y0", obj.y0 - value));
                    newProperties.Add(("y1", obj.y1 - value));
                }
            }

            // Create a new MyObject with the updated values
            var newObj = new Edge();
            newObj = obj;

            // Update the properties
            foreach (var prop in newProperties)
            {
                // You will need to use reflection or manual assignment for the dynamic property names
                switch (prop.Item1)
                {
                    case "x0":
                        newObj.x0 = prop.Item2;
                        break;
                    case "x1":
                        newObj.x1 = prop.Item2;
                        break;
                    case "top":
                        newObj.top = prop.Item2;
                        break;
                    case "bottom":
                        newObj.bottom = prop.Item2;
                        break;
                    case "doctop":
                        newObj.doctop = prop.Item2;
                        break;
                    case "y0":
                        newObj.y0 = prop.Item2;
                        break;
                    case "y1":
                        newObj.y1 = prop.Item2;
                        break;
                }
            }

            return newObj;
        }


        public static List<Edge> snap_objects(List<Edge> objs, string attr, float tolerance)
        {
            // Mapping the attribute to the axis (horizontal or vertical)
            string axis = attr switch
            {
                "x0" => "h",
                "x1" => "h",
                "top" => "v",
                "bottom" => "v",
                _ => throw new ArgumentException("Invalid attribute", nameof(attr))
            };

            List<List<Edge>> clusters = new List<List<Edge>>();
            List<float> avgs = new List<float>();
            List<List<Edge>> snappedClusters = new List<List<Edge>>();
            switch (attr)
            {
                case "x0":
                    clusters = cluster_objects(objs, obj => obj.x0, tolerance);
                    avgs = clusters.Select(cluster => cluster.Average(obj => obj.x0)).ToList();
                    snappedClusters = clusters.Select((cluster, idx) =>
                        cluster.Select(obj => move_object(obj, axis, avgs[idx] - (float)obj.x0)).ToList()).ToList();
                    break;
                case "x1":
                    clusters = cluster_objects(objs, obj => obj.x1, tolerance);
                    avgs = clusters.Select(cluster => cluster.Average(obj => obj.x1)).ToList();
                    snappedClusters = clusters.Select((cluster, idx) =>
                        cluster.Select(obj => move_object(obj, axis, avgs[idx] - (float)obj.x1)).ToList()).ToList();
                    break;
                case "top":
                    clusters = cluster_objects(objs, obj => obj.top, tolerance);
                    avgs = clusters.Select(cluster => cluster.Average(obj => obj.top)).ToList();
                    snappedClusters = clusters.Select((cluster, idx) =>
                        cluster.Select(obj => move_object(obj, axis, avgs[idx] - (float)obj.top)).ToList()).ToList();
                    break;
                case "bottom":
                    clusters = cluster_objects(objs, obj => obj.bottom, tolerance);
                    avgs = clusters.Select(cluster => cluster.Average(obj => obj.bottom)).ToList();
                    snappedClusters = clusters.Select((cluster, idx) =>
                        cluster.Select(obj => move_object(obj, axis, avgs[idx] - (float)obj.bottom)).ToList()).ToList();
                    break;
                default:
                    return null;
            }

            // Flatten the list of snapped clusters and return
            return snappedClusters.SelectMany(cluster => cluster).ToList();
        }

        // Given a list of edges, snap any within `tolerance` pixels of one another
        // to their positional average.
        public static List<Edge> snap_edges(
            List<Edge> edges,
            float xTolerance = 1.0f,
            float yTolerance = 1.0f)
        {
            // Group edges by orientation
            var byOrientation = new Dictionary<string, List<Edge>>()
            {
                { "v", new List<Edge>() },
                { "h", new List<Edge>() }
            };

            foreach (var edge in edges)
            {
                byOrientation[edge.orientation].Add(edge);
            }

            // Snap vertical and horizontal edges separately
            List<Edge> snappedV = snap_objects(byOrientation["v"], "x0", xTolerance);
            List<Edge> snappedH = snap_objects(byOrientation["h"], "top", yTolerance);

            // Combine and return snapped objects
            return snappedV.Concat(snappedH).ToList();
        }

        // Resize the object based on the given key and value
        public static Edge resize_object(Edge obj, string key, float value)
        {
            if (!new[] { "x0", "x1", "top", "bottom" }.Contains(key))
            {
                throw new ArgumentException("Invalid key. Must be one of 'x0', 'x1', 'top', 'bottom'.", nameof(key));
            }

            Edge newObj = new Edge();
            newObj = obj;

            if (key == "x0")
            {
                if (value > obj.x1) throw new ArgumentException("x0 must be less than or equal to x1.");
                newObj.x0 = value;
                newObj.width = obj.x1 - value;
            }
            else if (key == "x1")
            {
                if (value < obj.x0) throw new ArgumentException("x1 must be greater than or equal to x0.");
                newObj.x1 = value;
                newObj.width = value - obj.x0;
            }
            else if (key == "top")
            {
                if (value > obj.bottom) throw new ArgumentException("top must be less than or equal to bottom.");
                float oldValue = obj.top;
                float diff = value - oldValue;
                newObj.top = value;
                newObj.doctop = obj.doctop + diff;
                newObj.height = obj.height - diff;
                if (obj.y1 >= 0f) 
                    newObj.y1 = obj.y1 - diff;
            }
            else if (key == "bottom")
            {
                if (value < obj.top) throw new ArgumentException("bottom must be greater than or equal to top.");
                float oldValue = obj.bottom;
                float diff = value - oldValue;
                newObj.bottom = value;
                newObj.height = obj.height + diff;
                if (obj.y0 >= 0f) 
                    newObj.y0 = obj.y0 - diff;
            }

            // Return a new object with the updated properties
            return newObj;
        }

        // Given a list of edges along the same infinite line, join those that
        // are within `tolerance` pixels of one another.
        public static List<Edge> join_edge_group(List<Edge> edges, string orientation, float tolerance = TableFlags.TABLE_DEFAULT_JOIN_TOLERANCE)
        {
            List<Edge> joined = new List<Edge>();
            if (orientation == "h")
            {
                // Sort edges by the min property
                var sortedEdges = edges.OrderBy(e => e.x0).ToList();
                joined = new List<Edge> { sortedEdges[0] };

                foreach (var e in sortedEdges.Skip(1))
                {
                    var last = joined.Last();
                    if (e.x0 <= last.x1 + tolerance)
                    {
                        if (e.x1 > last.x1)
                        {
                            // Extend current edge to new extremity
                            joined[joined.Count - 1] = resize_object(last, "x1", e.x1);
                        }
                    }
                    else
                    {
                        // Edge is separate from the previous edge
                        joined.Add(e);
                    }
                }
            }
            else if (orientation == "v")
            {
                // Sort edges by the min property
                var sortedEdges = edges.OrderBy(e => e.top).ToList();
                joined = new List<Edge> { sortedEdges[0] };

                foreach (var e in sortedEdges.Skip(1))
                {
                    var last = joined.Last();
                    if (e.top <= last.bottom + tolerance)
                    {
                        if (e.bottom > last.bottom)
                        {
                            // Extend current edge to new extremity
                            joined[joined.Count - 1] = resize_object(last, "bottom", e.bottom);
                        }
                    }
                    else
                    {
                        // Edge is separate from the previous edge
                        joined.Add(e);
                    }
                }
            }
            else
            {
                throw new ArgumentException("Orientation must be 'v' or 'h'", nameof(orientation));
            }

            return joined;
        }

        // Using the `snap_edges` and `join_edge_group` methods above,
        // merge a list of edges into a more "seamless" list.
        public static List<Edge> merge_edges(
            List<Edge> edges,
            float snap_x_tolerance,
            float snap_y_tolerance,
            float join_x_tolerance,
            float join_y_tolerance)
        {
            // Snap edges if tolerance values are greater than 0
            if (snap_x_tolerance > 0 || snap_y_tolerance > 0)
            {
                edges = snap_edges(edges, snap_x_tolerance, snap_y_tolerance);
            }

            // Group edges by orientation
            var edgeGroups = edges
                .OrderBy(e => e.orientation == "h" ? e.top : e.x0)
                .GroupBy(e => e.orientation == "h" ? "h" : "v");

            // Join edges by their groups
            var joinedEdges = new List<Edge>();
            foreach (var group in edgeGroups)
            {
                float tolerance = group.Key == "h" ? join_x_tolerance : join_y_tolerance;
                joinedEdges.AddRange(join_edge_group(group.ToList(), group.Key, tolerance));
            }

            return joinedEdges;
        }

        // Return the rectangle(i.e a dict with keys "x0", "top", "x1",
        // "bottom") for an object.
        public static Dictionary<string, float> bbox_to_rect(BBox bbox)
        {
            var rect = new Dictionary<string, float>
            {
                { "x0", bbox.x0 },
                { "top", bbox.top },
                { "x1", bbox.x1 },
                { "bottom", bbox.bottom }
            };

            return rect;
        }

        // Given an iterable of objects, return the smallest rectangle(i.e.a
        // dict with "x0", "top", "x1", and "bottom" keys) that contains them
        // all.
        public static Dictionary<string, float> objects_to_rect(IEnumerable<object> objects)
        {
            BBox bbox = objects_to_bbox(objects);
            return bbox_to_rect(bbox);
        }

        // Given an iterable of bounding boxes, return the smallest bounding box
        // that contains them all.
        public static BBox merge_bboxes(List<BBox> bboxes)
        {
            var x0 = bboxes.Select(b => b.x0).Min();
            var top = bboxes.Select(b => b.top).Min();
            var x1 = bboxes.Select(b => b.x1).Max();
            var bottom = bboxes.Select(b => b.bottom).Max();

            return new BBox(x0, top, x1, bottom);
        }

        // Given an iterable of objects, return the smallest bounding box that
        // contains them all.
        public static BBox objects_to_bbox(IEnumerable<object> objects)
        {
            List<BBox> bboxes = new List<BBox>();
            foreach (var obj in objects)
            {
                if (obj is Character)
                {
                    Character ch = obj as Character;
                    bboxes.Add(new BBox(ch.x0, ch.top, ch.x1, ch.bottom));
                }
                else
                {
                    bboxes.Add(obj as BBox);
                }
            }
            return merge_bboxes(bboxes);
        }

        // Find(imaginary) horizontal lines that connect the tops
        // of at least `word_threshold` words.
        public static List<Edge> words_to_edges_h(List<Character> words, int wordThreshold = (int)TableFlags.TABLE_DEFAULT_MIN_WORDS_HORIZONTAL)
        {
            // Cluster the words by 'top' value (simulating `cluster_objects`)
            var byTop = cluster_objects(words, obj => obj.top, 1);

            // Filter clusters by the word threshold
            List<List<Character>> largeClusters = byTop.Where(cluster => cluster.Count >= wordThreshold).ToList();

            // Convert clusters to bounding rectangles
            var rects = largeClusters.Select(c => objects_to_bbox(c)).ToList();

            if (rects.Count == 0)
                return new List<Edge>();

            // Find min and max x0 and x1 values
            float minX0 = rects.Min(r => r.x0);
            float maxX1 = rects.Max(r => r.x1);

            List<Edge> edges = new List<Edge>();

            foreach (var r in rects)
            {
                // Add the 'top' edge for each detected row
                edges.Add(new Edge
                {
                    x0 = minX0,
                    x1 = maxX1,
                    top = r.top,
                    bottom = r.top,
                    width = maxX1 - minX0,
                    orientation = "h"
                });

                // Add the 'bottom' edge for each detected row (catches last row)
                edges.Add(new Edge
                {
                    x0 = minX0,
                    x1 = maxX1,
                    top = r.bottom,
                    bottom = r.bottom,
                    width = maxX1 - minX0,
                    orientation = "h"
                });
            }

            return edges;
        }

        public static BBox get_bbox_overlap(BBox a, BBox b)
        {
            float oLeft = Math.Max(a.x0, b.x0);
            float oRight = Math.Min(a.x1, b.x1);
            float oBottom = Math.Min(a.bottom, b.bottom);
            float oTop = Math.Max(a.top, b.top);

            float oWidth = oRight - oLeft;
            float oHeight = oBottom - oTop;

            if (oHeight >= 0 && oWidth >= 0 && oHeight + oWidth > 0)
            {
                return new BBox(oLeft, oTop, oRight, oBottom);
            }
            return null;
        }

        // Find(imaginary) vertical lines that connect the left, right, or
        // center of at least `word_threshold` words.
        public static List<Edge> words_to_edges_v(List<Character> words, int wordThreshold = (int)TableFlags.TABLE_DEFAULT_MIN_WORDS_VERTICAL)
        {
            // Find words that share the same left, right, or centerpoints
            var byX0 = cluster_objects(words, w => w.x0, 1);
            var byX1 = cluster_objects(words, w => w.x1, 1);
            var byCenter = cluster_objects(words, w => (w.x0 + w.x1) / 2, 1);

            var clusters = byX0.Concat(byX1).Concat(byCenter).ToList();

            // Find the points that align with the most words
            var sortedClusters = clusters.OrderByDescending(c => c.Count).ToList();
            var largeClusters = sortedClusters.Where(c => c.Count >= wordThreshold).ToList();

            // For each of those points, find the bboxes fitting all matching words
            var bboxes = largeClusters.Select(c => objects_to_bbox(c)).ToList();

            // Iterate through those bboxes, condensing overlapping bboxes
            var condensedBboxes = new List<BBox>();
            foreach (var bbox in bboxes)
            {
                bool overlap = condensedBboxes.Any(existingBbox => get_bbox_overlap(bbox, existingBbox) != null);
                if (!overlap)
                {
                    condensedBboxes.Add(bbox);
                }
            }

            if (condensedBboxes.Count == 0)
            {
                return new List<Edge>();
            }

            var condensedRects = condensedBboxes.Select(b => bbox_to_rect(b)).ToList();

            // Sort rectangles by x0.
            var sortedRects = condensedRects.OrderBy(r => r["x0"]).ToList();

            float maxX1 = sortedRects.Max(r => r["x1"]);
            float minTop = sortedRects.Min(r => r["top"]);
            float maxBottom = sortedRects.Max(r => r["bottom"]);

            // Create edges based on the rectangles.
            var edges = sortedRects.Select(b => new Edge
            {
                x0 = b["x0"],
                x1 = b["x0"],
                top = minTop,
                bottom = maxBottom,
                height = maxBottom - minTop,
                orientation = "v"
            }).ToList();

            edges.Add(new Edge
            {
                x0 = maxX1,
                x1 = maxX1,
                top = minTop,
                bottom = maxBottom,
                height = maxBottom - minTop,
                orientation = "v"
            });

            return edges;
        }

        // Given a list of edges, return the points at which they intersect
        // within `tolerance` pixels.
        public class Intersection
        {
            public float x0 { get; set; }
            public float top { get; set; }
            public float x1 { get; set; }
            public float bottom { get; set; }
            public List<Edge> VerticalEdges { get; set; }
            public List<Edge> HorizontalEdges { get; set; }

            public Intersection()
            {
                this.VerticalEdges = new List<Edge>();
                this.HorizontalEdges = new List<Edge>();
            }
        }

        public static Dictionary<Point, Intersection> edges_to_intersections(
            List<Edge> edges, float x_tolerance = 1.0f, float y_tolerance = 1.0f)
        {
            var intersections = new Dictionary<Point, Intersection>();

            // Separate vertical and horizontal edges
            var vEdges = edges.Where(e => e.orientation == "v").ToList();
            var hEdges = edges.Where(e => e.orientation == "h").ToList();

            // Sort edges (vertical by X0 then Top, horizontal by Top then X0)
            vEdges = vEdges.OrderBy(e => e.x0).ThenBy(e => e.top).ToList();
            hEdges = hEdges.OrderBy(e => e.top).ThenBy(e => e.x0).ToList();

            foreach (var v in vEdges)
            {
                foreach (var h in hEdges)
                {
                    // Check if the vertical and horizontal lines intersect within tolerance
                    if (v.top <= h.top + y_tolerance && v.bottom >= h.top - y_tolerance &&
                        v.x0 >= h.x0 - x_tolerance && v.x0 <= h.x1 + x_tolerance)
                    {
                        var vertex = new Point(v.x0, h.top);

                        if (!intersections.ContainsKey(vertex))
                        {
                            intersections[vertex] = new Intersection();
                        }

                        intersections[vertex].VerticalEdges.Add(v);
                        intersections[vertex].HorizontalEdges.Add(h);
                    }
                }
            }

            return intersections;
        }

        // Return the bounding box for an object.
        static BBox obj_to_bbox(Edge edge)
        {
            return new BBox(edge.x0, edge.top, edge.x1, edge.bottom);
        }


        // Given a list of points(`intersections`), return all rectangular "cells"
        // that those points describe.
        // `intersections` should be a dictionary with (x0, top) tuples as keys,
        // and a list of edge objects as values.The edge objects should correspond
        // to the edges that touch the intersection.
        public static List<BBox> intersections_to_cells(Dictionary<Point, Intersection> intersections)
        {
            var points = intersections.Keys.OrderBy(p => p).ToList();
            int nPoints = points.Count;

            bool edge_connects(Point p1, Point p2)
            {
                HashSet<BBox> edges_to_set(List<Edge> edges)
                {
                    return new HashSet<BBox>(edges.Select(obj_to_bbox));
                }

                if (p1.X == p2.X)
                {
                    var common = edges_to_set(intersections[p1].VerticalEdges).Intersect(edges_to_set(intersections[p2].VerticalEdges)).ToList();
                    if (common.Any()) return true;
                }

                if (p1.Y == p2.Y)
                {
                    var common = edges_to_set(intersections[p1].HorizontalEdges).Intersect(edges_to_set(intersections[p2].HorizontalEdges)).ToList();
                    if (common.Any()) return true;
                }

                return false;
            }

            BBox find_smallest_cell(int i)
            {
                if (i == nPoints - 1) return null;

                var pt = points[i];
                var rest = points.Skip(i + 1).ToList();

                // Get all the points directly below and directly right
                var below = rest.Where(x => x.X == pt.X).ToList();
                var right = rest.Where(x => x.Y == pt.Y).ToList();

                foreach (var belowPt in below)
                {
                    if (!edge_connects(pt, belowPt)) continue;

                    foreach (var rightPt in right)
                    {
                        if (!edge_connects(pt, rightPt)) continue;

                        Point bottomRight = new Point(rightPt.X, belowPt.Y);

                        if (intersections.ContainsKey(bottomRight)
                            && edge_connects(bottomRight, rightPt)
                            && edge_connects(bottomRight, belowPt))
                        {
                            return new BBox(pt.X, pt.Y, bottomRight.X, bottomRight.Y);
                        }
                    }
                }

                return null;
            }

            List<BBox> bBoxes = new List<BBox>();
            for (int i = 0; i < points.Count; i++)
            {
                BBox bbox = find_smallest_cell(i);
                if (bbox != null)
                    bBoxes.Add(bbox);
            }
            return bBoxes;
        }

        // Given a list of bounding boxes(`cells`), return a list of tables that
        // hold those cells most simply(and contiguously).
        public static List<List<BBox>> cells_to_tables(Page page, List<BBox> cells)
        {
            List<Point> bbox_to_corners(BBox bbox)
            {
                // Decompose the bounding box into its individual components
                float x0 = bbox.x0;
                float top = bbox.top;
                float x1 = bbox.x1;
                float bottom = bbox.bottom;

                // Return the four corners as a list of tuples
                return new List<Point>
                {
                    new Point(x0, top),
                    new Point(x0, bottom),
                    new Point(x1, top),
                    new Point(x1, bottom)
                };
            }

            List<BBox> remainingCells = new List<BBox>(cells);
            List<List<BBox>> tables = new List<List<BBox>>();

            // Iterate through the cells found above, and assign them
            // to contiguous tables
            HashSet<Point> currentCorners = new HashSet<Point>();
            List<BBox> currentCells = new List<BBox>();

            while (remainingCells.Count > 0)
            {
                int initialCellCount = currentCells.Count;

                foreach (var cell in new List<BBox>(remainingCells))
                {
                    List<Point> cellCorners = bbox_to_corners(cell);
                    // If we're just starting a table ...
                    if (currentCells.Count == 0)
                    {
                        // ... immediately assign it to the empty group
                        currentCorners.UnionWith(cellCorners);
                        currentCells.Add(cell);
                        remainingCells.Remove(cell);
                    }
                    else
                    {
                        // How many corners does this table share with the current group?
                        int cornerCount = cellCorners.Count(corner => currentCorners.Contains(corner));

                        // If touching on at least one corner...
                        if (cornerCount > 0)
                        {
                            // ... assign it to the current group
                            currentCorners.UnionWith(cellCorners);
                            currentCells.Add(cell);
                            remainingCells.Remove(cell);
                        }
                    }
                }
                // If this iteration did not find any more cells to append...
                if (currentCells.Count == initialCellCount)
                {
                    tables.Add(new List<BBox>(currentCells));
                    currentCorners.Clear();
                    currentCells.Clear();
                }
            }

            // Once we have exhausting the list of cells ...
            // ... and we have a cell group that has not been stored
            if (currentCells.Count > 0)
            {
                tables.Add(new List<BBox>(currentCells));
            }

            // remove tables without text or having only 1 column
            for (int i = tables.Count - 1; i >= 0; i--)
            {
                var r = new BBox(0, 0, 0, 0); // EMPTY_RECT placeholder
                var x1Vals = new HashSet<double>();
                var x0Vals = new HashSet<double>();

                foreach (var cell in tables[i])
                {
                    r |= cell;
                    x1Vals.Add(cell.x1);
                    x0Vals.Add(cell.x0);
                }

                string rText = page.GetTextbox(new Rect(r.x0, r.top, r.x1, r.bottom));
                if (x1Vals.Count < 2 || x0Vals.Count < 2 || whiteSpaces_issuperset(rText))
                {
                    tables.RemoveAt(i);
                }
            }

            // Sort the tables top-to-bottom-left-to-right based on the value of the
            // topmost-and-then-leftmost coordinate of a table.
            var sortedTables = tables.OrderBy(t => t.Min(c => c.top))
                                     .ThenBy(t => t.Min(c => c.x0))
                                     .ToList();

            return sortedTables;
        }

        public static List<Character> extract_words(List<Character> chars, Dictionary<string, object> kwargs)
        {
            // WordExtractor parameters
            float x_tolerance = TableFlags.TABLE_DEFAULT_X_TOLERANCE;
            float y_tolerance = TableFlags.TABLE_DEFAULT_Y_TOLERANCE;
            bool keep_blank_chars = false;
            bool use_text_flow = false;
            bool horizontal_ltr = true;
            bool vertical_ttb = false;
            List<string> extra_attrs = null;
            bool split_at_punctuation = false;
            bool expand_ligatures = true;

            foreach (string key in kwargs.Keys)
            {
                switch (key)
                {
                    case "x_tolerance":
                        x_tolerance = float.Parse(kwargs[key].ToString()); break;
                    case "y_tolerance":
                        y_tolerance = float.Parse(kwargs[key].ToString()); break;
                    case "keep_blank_chars":
                        keep_blank_chars = bool.Parse(kwargs[key].ToString()); break;
                    case "use_text_flow":
                        use_text_flow = bool.Parse(kwargs[key].ToString()); break;
                    case "horizontal_ltr":
                        horizontal_ltr = bool.Parse(kwargs[key].ToString()); break;
                    case "vertical_ttb":
                        vertical_ttb = bool.Parse(kwargs[key].ToString()); break;
                    case "extra_attrs":
                        extra_attrs = (List<string>)kwargs[key]; break;
                    case "split_at_punctuation":
                        split_at_punctuation = bool.Parse(kwargs[key].ToString()); break;
                    case "expand_ligatures":
                        expand_ligatures = bool.Parse(kwargs[key].ToString()); break;
                    default:
                        break;
                }
            }

            WordExtractor extractor = new WordExtractor(
                x_tolerance,
                y_tolerance,
                keep_blank_chars,
                use_text_flow,
                horizontal_ltr,
                vertical_ttb,
                extra_attrs,
                split_at_punctuation,
                expand_ligatures
            );
            
            return extractor.extract_words(chars);
        }

        public static TextMap chars_to_textmap(List<Character> chars, Dictionary<string, object> kwargs)
        {
            // Add the presorted parameter
            kwargs["presorted"] = true;

            // WordExtractor parameters
            float x_tolerance = TableFlags.TABLE_DEFAULT_X_TOLERANCE;
            float y_tolerance = TableFlags.TABLE_DEFAULT_Y_TOLERANCE;
            bool keep_blank_chars = false;
            bool use_text_flow = false;
            bool horizontal_ltr = true;
            bool vertical_ttb = false;
            List<string> extra_attrs = null;
            bool split_at_punctuation = false;
            bool expand_ligatures = true;

            // WordMap parameters
            bool layout = false;
            float layout_width = 0f;
            float layout_height = 0f;
            int layout_width_chars = 0;
            int layout_height_chars = 0;
            float x_density = TableFlags.TABLE_DEFAULT_X_DENSITY;
            float y_density = TableFlags.TABLE_DEFAULT_Y_DENSITY;
            float x_shift = 0;
            float y_shift = 0;
            bool presorted = false;

            foreach (string key in kwargs.Keys)
            {
                switch (key)
                {
                    case "x_tolerance":
                        x_tolerance = (float)kwargs[key]; break;
                    case "y_tolerance":
                        y_tolerance = (float)kwargs[key]; break;
                    case "keep_blank_chars":
                        keep_blank_chars = (bool)kwargs[key]; break;
                    case "use_text_flow":
                        use_text_flow = (bool)kwargs[key]; break;
                    case "horizontal_ltr":
                        horizontal_ltr = (bool)kwargs[key]; break;
                    case "vertical_ttb":
                        vertical_ttb = (bool)kwargs[key]; break;
                    case "extra_attrs":
                        extra_attrs = (List<string>)kwargs[key]; break;
                    case "split_at_punctuation":
                        split_at_punctuation = (bool)kwargs[key]; break;
                    case "expand_ligatures":
                        expand_ligatures = (bool)kwargs[key]; break;
                    case "layout":
                        layout = (bool)kwargs[key]; break;
                    case "layout_width":
                        layout_width = (float)kwargs[key]; break;
                    case "layout_height":
                        layout_height = (float)kwargs[key]; break;
                    case "layout_width_chars":
                        layout_width_chars = (int)kwargs[key]; break;
                    case "layout_height_chars":
                        layout_height_chars = (int)kwargs[key]; break;
                    case "x_density":
                        x_density = (float)kwargs[key]; break;
                    case "y_density":
                        y_density = (float)kwargs[key]; break;
                    case "x_shift":
                        x_shift = (float)kwargs[key]; break;
                    case "y_shift":
                        y_shift = (float)kwargs[key]; break;
                    case "presorted":
                        presorted = (bool)kwargs[key]; break;
                    default:
                        break;
                }
            }

            WordExtractor extractor = new WordExtractor(
                x_tolerance,
                y_tolerance,
                keep_blank_chars,
                use_text_flow,
                horizontal_ltr,
                vertical_ttb,
                extra_attrs,
                split_at_punctuation,
                expand_ligatures
            );

            WordMap wordmap = extractor.extract_wordmap(chars);

            TextMap textmap = wordmap.to_textmap(
                layout,
                layout_width,
                layout_height,
                layout_width_chars,
                layout_height_chars,
                x_density,
                y_density,
                x_shift,
                y_shift,
                y_tolerance,
                use_text_flow,
                presorted,
                expand_ligatures
            );

            return textmap;
        }

        public static string extract_text(List<Character> chars, Dictionary<string, object> kwargs)
        {
            // WordExtractor parameters
            float x_tolerance = TableFlags.TABLE_DEFAULT_X_TOLERANCE;
            float y_tolerance = TableFlags.TABLE_DEFAULT_Y_TOLERANCE;
            bool keep_blank_chars = false;
            bool use_text_flow = false;
            bool horizontal_ltr = true;
            bool vertical_ttb = false;
            List<string> extra_attrs = null;
            bool split_at_punctuation = false;
            bool expand_ligatures = true;

            // WordMap parameters
            bool layout = false;
            float layout_width = 0f;
            float layout_height = 0f;
            int layout_width_chars = 0;
            int layout_height_chars = 0;
            float x_density = TableFlags.TABLE_DEFAULT_X_DENSITY;
            float y_density = TableFlags.TABLE_DEFAULT_Y_DENSITY;
            float x_shift = 0;
            float y_shift = 0;
            bool presorted = false;

            foreach (string key in kwargs.Keys)
            {
                switch (key)
                {
                    case "x_tolerance":
                        x_tolerance = (float)kwargs[key]; break;
                    case "y_tolerance":
                        y_tolerance = (float)kwargs[key]; break;
                    case "keep_blank_chars":
                        keep_blank_chars = (bool)kwargs[key]; break;
                    case "use_text_flow":
                        use_text_flow = (bool)kwargs[key]; break;
                    case "horizontal_ltr":
                        horizontal_ltr = (bool)kwargs[key]; break;
                    case "vertical_ttb":
                        vertical_ttb = (bool)kwargs[key]; break;
                    case "extra_attrs":
                        extra_attrs = (List<string>)kwargs[key]; break;
                    case "split_at_punctuation":
                        split_at_punctuation = (bool)kwargs[key]; break;
                    case "expand_ligatures":
                        expand_ligatures = (bool)kwargs[key]; break;
                    case "layout":
                        layout = (bool)kwargs[key]; break;
                    case "layout_width":
                        layout_width = (float)kwargs[key]; break;
                    case "layout_height":
                        layout_height = (float)kwargs[key]; break;
                    case "layout_width_chars":
                        layout_width_chars = (int)kwargs[key]; break;
                    case "layout_height_chars":
                        layout_height_chars = (int)kwargs[key]; break;
                    case "x_density":
                        x_density = (float)kwargs[key]; break;
                    case "y_density":
                        y_density = (float)kwargs[key]; break;
                    case "x_shift":
                        x_shift = (float)kwargs[key]; break;
                    case "y_shift":
                        y_shift = (float)kwargs[key]; break;
                    case "presorted":
                        presorted = (bool)kwargs[key]; break;
                    default:
                        break;
                }
            }

            if (chars.Count == 0)
            {
                return "";
            }

            // Layout handling
            if (layout == true)
            {
                return chars_to_textmap(chars, kwargs).AsString;
            }
            else
            {
                WordExtractor extractor = new WordExtractor(
                    x_tolerance,
                    y_tolerance,
                    keep_blank_chars,
                    use_text_flow,
                    horizontal_ltr,
                    vertical_ttb,
                    extra_attrs,
                    split_at_punctuation,
                    expand_ligatures
                );

                // Extract words using WordExtractor
                List<Character> words = extractor.extract_words(chars);
                // rotation cannot change within a cell
                int rotation = words.Count > 0 ? (int)words[0].rotation : 0;

                string lines;

                if (rotation == 90)
                {
                    // Sort for rotation 90
                    words = words.OrderBy(w => w.x1).ThenByDescending(w => w.top).ToList();
                    lines = string.Join(" ", words.Select(w => w.text.ToString()));
                }
                else if (rotation == 270)
                {
                    // Sort for rotation 270
                    words = words.OrderByDescending(w => w.x1).ThenBy(w => w.top).ToList();
                    lines = string.Join(" ", words.Select(w => w.text.ToString()));
                }
                else
                {
                    // Cluster words based on doctop
                    var linesGrouped = cluster_objects(words, obj=>obj.doctop, y_tolerance);
                    lines = string.Join("\n", linesGrouped.Select(line => string.Join(" ", line.Select(w => w.text))));

                    if (rotation == 180)
                    {
                        // Special handling for rotation 180 (reverse lines and replace newline with spaces)
                        lines = new string(lines.Reverse().Select(c => c == '\n' ? ' ' : c).ToArray());
                    }
                }

                return lines;
            }
        }
    }

    public class TextItem
    {
        public string Text { get; set; }
        public object Obj { get; set; }

        public TextItem(string text, object obj)
        {
            Text = text;
            Obj = obj;
        }
    }

    public class TextMap
    {
        public List<TextItem> Tuples { get; set; }
        public string AsString { get; set; }

        public TextMap(List<TextItem> tuples = null)
        {
            Tuples = tuples ?? new List<TextItem>();
            AsString = string.Join("", Tuples.Select(item => item.Text));
        }
    }

    public class WordMap
    {
        public List<Tuple<Character, List<Character>>> Tuples { get; set; }

        public WordMap(List<Tuple<Character, List<Character>>> tuples)
        {
            Tuples = tuples;
        }

        public TextMap to_textmap(
            bool layout = false,
            float layoutWidth = 0,
            float layoutHeight = 0,
            int layoutWidthChars = 0,
            int layoutHeightChars = 0,
            float xDensity = TableFlags.TABLE_DEFAULT_X_DENSITY,
            float yDensity = TableFlags.TABLE_DEFAULT_Y_DENSITY,
            float xShift = 0,
            float yShift = 0,
            float yTolerance = TableFlags.TABLE_DEFAULT_Y_TOLERANCE,
            bool useTextFlow = false,
            bool presorted = false,
            bool expandLigatures = true
        )
        {
            var textMap = new List<TextItem>();

            if (Tuples.Count == 0)
                return new TextMap(textMap);

            var expansions = expandLigatures ? TableFlags.TABLE_LIGATURES : new Dictionary<string, string>();

            // Layout handling
            if (layout)
            {
                if (layoutWidthChars > 0)
                {
                    if (layoutWidth > 0)
                    {
                        throw new ArgumentException("`layoutWidth` and `layoutWidthChars` cannot both be set.");
                    }
                }
                else
                {
                    layoutWidthChars = (int)Math.Round(layoutWidth / xDensity);
                }

                if (layoutHeightChars > 0)
                {
                    if (layoutHeight > 0)
                    {
                        throw new ArgumentException("`layoutHeight` and `layoutHeightChars` cannot both be set.");
                    }
                }
                else
                {
                    layoutHeightChars = (int)Math.Round(layoutHeight / yDensity);
                }
            }

            int numNewlines = 0;
            var wordsSortedDoctop = presorted || useTextFlow
                ? Tuples
                : Tuples.OrderBy(t => t.Item1.doctop).ToList();

            Character firstWord = wordsSortedDoctop[0].Item1;
            float doctopStart = firstWord.doctop - firstWord.top;

            int k = 0;
            foreach (var ws in cluster_objects(wordsSortedDoctop, t => t.Item1.doctop, yTolerance))
            {
                float yDist = layout
                    ? (ws[0].Item1.doctop - (doctopStart + yShift)) / yDensity
                    : 0;

                int numNewlinesPrepend = Math.Max(k > 0 ? 1 : 0, (int)Math.Round(yDist) - numNewlines);
                k++;
                for (int i = 0; i < numNewlinesPrepend; i++)
                {
                    if (textMap.Count == 0 || textMap.Last().Text == "\n")
                    {
                        textMap.Add(new TextItem(" ", null));  // Blank line handling
                    }
                    textMap.Add(new TextItem("\n", null));  // Add newline
                }
                numNewlines += numNewlinesPrepend;

                float lineLen = 0;

                var lineWordsSortedX0 = presorted || useTextFlow
                    ? ws
                    : ws.OrderBy(w => w.Item1.x0).ToList();

                foreach (var word in lineWordsSortedX0)
                {
                    var wordObj = word.Item1;
                    float xDist = layout ? (wordObj.x0 - xShift) / xDensity : 0;
                    int numSpacesPrepend = Math.Max(Math.Min(1, (int)lineLen), (int)Math.Round(xDist) - (int)lineLen);
                    for (int i = 0; i < numSpacesPrepend; i++)
                    {
                        textMap.Add(new TextItem(" ", null));  // Add spaces before the word
                    }
                    lineLen += numSpacesPrepend;

                    foreach (Character c in word.Item2)
                    {
                        string letters = expansions.ContainsKey(c.text) ? expansions[c.text] : c.text;
                        foreach (var letter in letters)
                        {
                            textMap.Add(new TextItem(letter.ToString(), c));  // Add each letter
                            lineLen += 1;
                        }
                    }
                }

                // Add spaces at the end of the line if layout
                if (layout)
                {
                    for (int i = 0; i < (layoutWidthChars - (int)lineLen); i++)
                    {
                        textMap.Add(new TextItem(" ", null));
                    }
                }
            }

            // Append blank lines at the end of text
            if (layout)
            {
                int numNewlinesAppend = layoutHeightChars - (numNewlines + 1);
                for (int i = 0; i < numNewlinesAppend; i++)
                {
                    if (i > 0)
                    {
                        textMap.Add(new TextItem(" ", null));  // Blank line at the end
                    }
                    textMap.Add(new TextItem("\n", null));  // Add newline
                }

                // Remove the last newline if present
                if (textMap.Last().Text == "\n")
                {
                    textMap.RemoveAt(textMap.Count - 1);
                }
            }

            return new TextMap(textMap);
        }
    }

    public class WordExtractor
    {
        public float xTolerance;
        public float yTolerance;
        public bool keepBlankChars;
        public bool useTextFlow;
        public bool horizontalLtr; // Should words be read left-to-right?
        public bool verticalTtb;   // Should vertical words be read top-to-bottom?
        public List<string> extraAttrs;
        public string splitAtPunctuation;
        public Dictionary<string, string> expansions;

        public WordExtractor(
            float xTolerance = TableFlags.TABLE_DEFAULT_X_TOLERANCE,
            float yTolerance = TableFlags.TABLE_DEFAULT_Y_TOLERANCE,
            bool keepBlankChars = false,
            bool useTextFlow = false,
            bool horizontalLtr = true,
            bool verticalTtb = false,
            List<string> extraAttrs = null,
            bool splitAtPunctuation = false,
            bool expandLigatures = true
        )
        {
            this.xTolerance = xTolerance;
            this.yTolerance = yTolerance;
            this.keepBlankChars = keepBlankChars;
            this.useTextFlow = useTextFlow;
            this.horizontalLtr = horizontalLtr;
            this.verticalTtb = verticalTtb;
            this.extraAttrs = extraAttrs ?? new List<string>();
            this.splitAtPunctuation = splitAtPunctuation ? string.Join("", new[] { '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/', ':', ';', '<', '=', '>', '?', '@', '[', '\\', ']', '^', '_', '`', '{', '|', '}', '~' }) : "";
            this.expansions = expandLigatures ? TableFlags.TABLE_LIGATURES : new Dictionary<string, string>();
        }

        public Character merge_chars(List<Character> orderedChars)
        {
            float x0, top, x1, bottom;
            BBox bbox = objects_to_bbox(orderedChars);
            x0 = bbox.x0; top = bbox.top; x1 = bbox.x1; bottom = bbox.bottom;
            float doctopAdj = orderedChars[0].doctop - orderedChars[0].top;
            bool upright = orderedChars[0].upright;
            int direction = (this.horizontalLtr ? 1 : -1) * (upright ? 1 : -1);

            Matrix matrix = orderedChars[0].matrix;

            int rotation = 0;
            if (!upright && matrix[1] < 0)
            {
                orderedChars.Reverse();
                rotation = 270;
            }
            else if (matrix[0] < 0 && matrix[3] < 0)
            {
                rotation = 180;
            }
            else if (matrix[1] > 0)
            {
                rotation = 90;
            }

            var word = new Character { 
                text = string.Join("", orderedChars.Select(c => expansions.ContainsKey(c.text) ? expansions[c.text] : c.text)),
                x0 = x0,
                x1 = x1,
                top = top,
                doctop = top + doctopAdj,
                bottom = bottom,
                upright = upright,
                direction = direction,
                rotation = rotation
            };

            foreach (var key in this.extraAttrs)
            {
                var val = orderedChars[0].GetType().GetProperty(key).GetValue(orderedChars[0]);
                word.GetType().GetProperty(key).SetValue(word, val);
            }

            return word;
        }

        // This method takes several factors into account to determine if
        // `curr_char` represents the beginning of a new word:
        // - Whether the text is "upright" (i.e., non-rotated)
        // - Whether the user has specified that horizontal text runs
        //   left-to-right(default) or right-to-left, as represented by
        //   self.horizontal_ltr
        // - Whether the user has specified that vertical text the text runs
        //   top-to-bottom(default) or bottom-to-top, as represented by
        //   self.vertical_ttb
        // - The x0, top, x1, and bottom attributes of prev_char and
        //   curr_char
        // - The self.x_tolerance and self.y_tolerance settings. Note: In
        //   this case, x/y refer to those directions for non-rotated text.
        //   For vertical text, they are flipped.A more accurate terminology
        //   might be "*intra*line character distance tolerance" and
        //   "*inter*line character distance tolerance"
        // An important note: The* intra*line distance is measured from the
        // * end* of the previous character to the *beginning* of the current
        // character, while the* inter*line distance is measured from the
        // * top* of the previous character to the *top* of the next
        // character.The reasons for this are partly repository-historical,
        // and partly logical, as successive text lines' bounding boxes often
        // overlap slightly (and we don't want that overlap to be interpreted
        // as the two lines being the same line).
        // The upright-ness of the character determines the attributes to
        // compare, while horizontal_ltr/vertical_ttb determine the direction
        // of the comparison.
        public bool char_begins_new_word(Character prevChar, Character currChar)
        {
            float x, y, ay, cy, ax, bx, cx;
        
            // Note: Due to the grouping step earlier in the process,
            // curr_char["upright"] will always equal prev_char["upright"].
            if (currChar.upright == true)
            {
                x = this.xTolerance;
                y = this.yTolerance;
                ay = prevChar.top;
                cy = currChar.top;
                if (horizontalLtr == true)
                {
                    ax = prevChar.x0;
                    bx = prevChar.x1;
                    cx = currChar.x0;
                }
                else 
                {
                    ax = -prevChar.x1;
                    bx = -prevChar.x0;
                    cx = -currChar.x1;
                }
            }
            else
            {
                x = this.yTolerance;
                y = this.xTolerance;
                ay = prevChar.x0;
                cy = currChar.x0;
                if (verticalTtb == true)
                {
                    ax = prevChar.top;
                    bx = prevChar.bottom;
                    cx = currChar.top;
                }
                else
                {
                    ax = -prevChar.bottom;
                    bx = -prevChar.top;
                    cx = -currChar.bottom;
                }
            }

            return (cx < ax) || (cx > bx + x) || (cy > ay + y);
        }

        public IEnumerable<List<Character>> iter_chars_to_words(List<Character> orderedChars)
        {
            List<Character> currentWord = new List<Character>();

            foreach (var charDict in orderedChars)
            {
                string text = charDict.text;

                // If keep_blank_chars is false and the char is a space, we start the next word
                if (!this.keepBlankChars && string.IsNullOrWhiteSpace(text))
                {
                    yield return currentWord; // Yield the current word
                    currentWord.Clear();
                }

                // If text is a punctuation mark, split the word
                else if (this.splitAtPunctuation.Contains(text))
                {
                    yield return currentWord; // Yield the current word
                    currentWord.Clear();
                    currentWord.Add(charDict);  // Add punctuation as a new word
                    yield return currentWord;  // Yield the punctuation as a word
                    currentWord.Clear();
                }
                // Check if this character begins a new word
                else if (currentWord.Count > 0 && char_begins_new_word(currentWord[currentWord.Count - 1], charDict))
                {
                    yield return currentWord; // Yield the current word
                    currentWord.Clear();
                    currentWord.Add(charDict);  // Start a new word with this char
                }
                else
                {
                    currentWord.Add(charDict);  // Otherwise, just add the character to the current word
                }
            }

            // Yield the last word if it exists
            if (currentWord.Count > 0)
            {
                yield return currentWord;
            }
        }

        public IEnumerable<Character> iter_sort_chars(List<Character> chars)
        {
            Func<Character, int> upright_key = x => -Convert.ToInt32(x.upright);

            // Sort characters based on "upright"
            var uprightClusters = chars
                .GroupBy(x => x.upright)
                .OrderByDescending(g => g.Key) // Group by "upright" key (1 for upright, 0 for non-upright)
                .ToList();

            foreach (var uprightCluster in uprightClusters)
            {
                bool upright = uprightCluster.Key;
                string clusterKey = upright ? "doctop" : "x0"; // Define clustering key based on upright status

                // Cluster by line using "doctop" for upright or "x0" for non-upright characters
                var subclusters = uprightCluster
                    .GroupBy(c => upright ? c.doctop : c.x0)
                    .OrderBy(g => g.Key)
                    .ToList();

                foreach (var subcluster in subclusters)
                {
                    // Sort within each subcluster
                    var sortedChars = subcluster.OrderBy(c => upright ? c.x0 : c.doctop).ToList();

                    // Reverse order if necessary
                    if (!(horizontalLtr && upright || verticalTtb && !upright))
                    {
                        sortedChars.Reverse();
                    }

                    // Yield the sorted characters
                    foreach (var character in sortedChars)
                    {
                        yield return character;
                    }
                }
            }
        }

        public IEnumerable<Tuple<Character, List<Character>>> iter_extract_tuples(List<Character> chars)
        {
            // Sort characters if necessary
            var orderedChars = useTextFlow ? chars : iter_sort_chars(chars).ToList();

            // Group characters by "Upright" and any extra attributes
            var groupedChars = orderedChars
                .GroupBy(c => new { c.upright, ExtraAttrs = string.Join(",", extraAttrs.Select(attr => attr)) })
                .ToList();

            foreach (var group in groupedChars)
            {
                var charGroup = group.ToList(); // All characters in this group

                // Assuming we have a method to split characters into words
                foreach (var wordChars in iter_chars_to_words(charGroup))
                {
                    // Yield the word (merged characters and the list of characters)
                    yield return new Tuple<Character, List<Character>>(merge_chars(wordChars), wordChars);
                }
            }
        }

        public WordMap extract_wordmap(List<Character> chars)
        {
            // Convert the result of IterExtractTuples into a list of tuples and return a WordMap
            return new WordMap(iter_extract_tuples(chars).ToList());
        }

        public List<Character> extract_words(List<Character> chars)
        {
            // Extract words by iterating over the tuples and selecting the first item (the word)
            var words = iter_extract_tuples(chars)
                .Select(tuple => tuple.Item1)  // Select the word (first item in the tuple)
                .ToList();

            return words;
        }
    }
    
    public class CellGroup
    {
        public List<BBox> Cells { get; set; }  // List of tuples representing the bounding boxes

        public BBox Bbox { get; set; }

        public CellGroup(List<BBox> cells)
        {
            Cells = cells;

            // Filter out null cells and then calculate the bounding box (bbox)
            var filteredCells = cells.Where(cell => cell != null).ToList();

            // Calculate the bounding box using LINQ (equivalent to min/max in Python)
            Bbox = new BBox(
                filteredCells.Min(cell => cell.x0),  // min x0
                filteredCells.Min(cell => cell.top),  // min top
                filteredCells.Max(cell => cell.x1),  // max x1
                filteredCells.Max(cell => cell.bottom)   // max bottom
            );
        }
    }

    public class TableRow : CellGroup
    {
        // Inherits everything from CellGroup and does not add any new behavior yet.
        public TableRow(List<BBox> cells) : base(cells)
        {
        }
    }

    public class TableHeader
    {
        // Properties to hold the bounding box, cells, names, and above (external)
        public BBox Bbox { get; set; }
        public List<BBox> Cells { get; set; }
        public List<string> Names { get; set; }
        public bool External { get; set; }  // Use 'object' if 'above' can be of different types

        // Constructor
        public TableHeader(BBox bbox, List<BBox> cells, List<string> names, bool above)
        {
            Bbox = bbox;
            Cells = cells;
            Names = names;
            External = above;
        }
    }

    public class Table
    {
        public Page Page { get; set; }  // Represents the page object in your document
        public List<BBox> Cells { get; set; }
        public TableHeader Header { get; set; }
        public List<Character> Chars { get; set; }

        public Table(Page page, List<BBox> cells, List<Character> chars)
        {
            this.Page = page;
            this.Cells = cells;
            this.Header = _get_header();
            this.Chars = chars;
        }

        public BBox Bbox
        {
            get
            {
                var c = this.Cells;
                return new BBox(
                    c.Min(cell => cell.x0),
                    c.Min(cell => cell.top),
                    c.Max(cell => cell.x1),
                    c.Max(cell => cell.bottom)
                );
            }
        }

        public List<TableRow> Rows
        {
            get
            {
                var sorted = this.Cells.OrderBy(cell => cell.top).ThenBy(cell => cell.x0).ToList();
                var xCoordinates = sorted.Select(cell => cell.x0).Distinct().OrderBy(x => x).ToList();
                var rows = new List<TableRow>();

                foreach (var group in sorted.GroupBy(cell => cell.top))
                {
                    var rowCells = group.ToDictionary(cell => cell.x0, cell => cell);
                    var row = new TableRow(rowCells.Values.ToList());
                    rows.Add(row);
                }

                return rows;
            }
        }

        public int RowCount => Rows.Count;
        public int ColCount => Rows.Max(row => row.Cells.Count);

        public List<List<string>> extract(Dictionary<string, object> kwargs = null)
        {
            var chars = Chars;  // Placeholder for actual char extraction logic
            var tableArr = new List<List<string>>();

            bool char_in_bbox(Character character, BBox bbox)
            {
                // Calculate the vertical and horizontal midpoints of the character's bounding box
                float vMid = (character.top + character.bottom) / 2;
                float hMid = (character.x0 + character.x1) / 2;

                // Get the coordinates from the bounding box
                float x0 = bbox.x0;
                float top = bbox.top;
                float x1 = bbox.x1;
                float bottom = bbox.bottom;

                // Check if the character's midpoint is within the bounding box
                return (hMid >= x0 && hMid < x1 && vMid >= top && vMid < bottom);
            }

            foreach (var row in Rows)
            {
                var rowArr = new List<string>();
                var rowChars = chars.Where(c => char_in_bbox(c, row.Bbox)).ToList();

                foreach (BBox cell in row.Cells)
                {
                    string cellText = string.Empty;
                    if (cell != null)
                    {
                        var cellChars = rowChars.Where(c => char_in_bbox(c, cell)).ToList();
                        if (cellChars.Any())
                        {
                            kwargs["x_shift"] = cell.x0;
                            kwargs["y_shift"] = cell.top;

                            // Check if "layout" is in kwargs and update layout_width and layout_height accordingly
                            if (kwargs.ContainsKey("layout"))
                            {
                                kwargs["layout_width"] = cell.x1 - cell.x0;
                                kwargs["layout_height"] = cell.bottom - cell.top;
                            }
                            // Call your text extraction logic here
                            cellText = extract_text(cellChars, kwargs);
                        }
                        else
                        {
                            cellText = string.Empty;
                        }
                    }
                    rowArr.Add(cellText);
                }
                tableArr.Add(rowArr);
            }

            return tableArr;
        }

        // Output table content as a string in Github-markdown format.
        // If clean is true, markdown syntax is removed from cell content.
        public string to_markdown(bool clean = true)
        {
            StringBuilder output = new StringBuilder("|");

            // Generate header string and MD underline
            for (int i = 0; i < Header.Names.Count; i++)
            {
                string name = Header.Names[i];
                if (string.IsNullOrEmpty(name))  // Generate a name if empty
                {
                    name = $"Col{i + 1}";
                }

                name = name.Replace("\n", " ");  // Remove any line breaks

                if (clean)  // Remove sensitive syntax
                {
                    name = WebUtility.HtmlEncode(name.Replace("-", "&#45;"));
                }

                output.Append(name + "|");
            }

            output.Append("\n");

            // Generate the markdown header line
            for (int i = 0; i < ColCount; i++)
            {
                output.Append("---|");
            }
            output.Append("\n");

            // Skip first row in details if header is part of the table
            int j = (Header.External ? 0 : 1);

            // Iterate over detail rows
            var rows = extract();  // Assuming Extract() is a method that returns a List<List<string>>
            foreach (var row in rows.GetRange(j, rows.Count - j))
            {
                string line = "|";
                foreach (var cell in row)
                {
                    // Output null cells with empty string
                    string cellContent = cell ?? "";
                    cellContent = cellContent.Replace("\n", " ");  // Remove line breaks
                    if (clean)  // Remove sensitive syntax
                    {
                        cellContent = WebUtility.HtmlEncode(cellContent.Replace("-", "&#45;"));
                    }
                    line += cellContent + "|";
                }
                line += "\n";
                output.Append(line);
            }

            return output.ToString() + "\n";
        }
        
        // Identify the table header.
        // *** PyMuPDF extension. ***
        // Starting from the first line above the table upwards, check if it
        // qualifies to be part of the table header.
        // Criteria include:
        // * A one-line table never has an extra header.
        // * Column borders must not intersect any word. If this happens, all
        //   text of this line and above of it is ignored.
        // * No excess inter-line distance: If a line further up has a distance
        //   of more than 1.5 times of its font size, it will be ignored and
        //   all lines above of it.
        // * Must have same text properties.
        // * Starting with the top table line, a bold text property cannot change
        // back to non-bold.
        // If not all criteria are met (or there is no text above the table),
        // the first table row is assumed to be the header.
        private TableHeader _get_header(int yTolerance = 3)
        {
            // Check if row 0 has bold text anywhere.
            // If this is true, then any non - bold text in lines above disqualify
            // these lines as header.
            // bbox is the(potentially repaired) row 0 bbox.
            // Returns True or False
            bool top_row_is_bold(BBox bbox)
            {
                List<Block> blocks = Page.GetText("dict", clip: new Rect(bbox.x0, bbox.top, bbox.x1, bbox.bottom),
                    flags: (int)TextFlagsExtension.TEXTFLAGS_TEXT).Blocks;
                foreach (Block block in blocks)
                {
                    foreach (Line line in block.Lines)
                    {
                        foreach (Span span in line.Spans)
                        {
                            if (((int)span.Flags & 16) != 0)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            if (Rows.Count == 0)
            {
                return null;
            }

            var row = Rows[0];
            var cells = row.Cells;
            var bbox = new BBox(row.Bbox.x0, row.Bbox.top, row.Bbox.x1, row.Bbox.bottom);

            TableHeader headerTopRow = new TableHeader(bbox, cells, extract()[0], false);

            // One-line tables have no extra header
            if (Rows.Count < 2)
                return headerTopRow;

            if (cells.Count < 2)
                return headerTopRow;

            // column (x) coordinates
            var colX = new List<float>();
            foreach (var cell in cells.Take(cells.Count - 1))
            {
                if (cell != null)
                {
                    colX.Add(cell.x1); // Assuming X1 is the right edge of the cell
                }
            }

            // Special check: is top row bold?
            // If first line above table is not bold, but top-left table cell is bold,
            // we take first table row as header
            bool topRowBold = top_row_is_bold(bbox);

            // clip = area above table
            // We will inspect this area for text qualifying as column header.
            BBox clip = new BBox(bbox.x0, bbox.top, bbox.x1, bbox.bottom);
            clip.top = 0; // Start at the top of the page
            clip.bottom = bbox.top; // End at the top of the table

            var spans = new List<Span>();
            List<Block> clipBlocks = Page.GetText("dist", clip:new Rect(clip.x0, clip.top, clip.x1, clip.bottom), flags: (int)TextFlagsExtension.TEXTFLAGS_TEXT).Blocks;
            foreach (Block block in clipBlocks)
            {
                foreach (Line line in block.Lines)
                {
                    foreach (Span span in line.Spans)
                    {
                        int flag = (int)span.Flags;
                        if ((flag & 1) == 0 && !string.IsNullOrWhiteSpace(span.Text))
                        {  // ignore superscripts and empty text
                            spans.Add(span);
                        }
                    }
                }
            }

            var select = new List<float>(); // y1 coordinates above, sorted descending
            var lineHeights = new List<float>();    // line heights above, sorted descending
            var lineBolds = new List<bool>();   // bold indicator per line above, same sorting

            // spans sorted descending
            spans = spans.OrderByDescending(s => s.Bbox.Y1).ToList();

            // walk through the spans and fill above 3 lists
            for (int i = 0; i < spans.Count; i++)
            {
                Span span = spans[i];
                float y1 = span.Bbox.Y1;  // span bottom
                float height = y1 - span.Bbox.Y0; // span bbox height
                bool bold = ((int)span.Flags & 16) != 0;

                // use first item to start the lists
                if (i == 0)
                {
                    select.Add(y1);
                    lineHeights.Add(height);
                    lineBolds.Add(bold);
                    continue;
                }

                // get last items from the 3 lists
                float y0 = select.Last();
                float prevHeight = lineHeights.Last();
                bool prevBold = lineBolds.Last();

                if (prevBold && !bold)
                    break;  // stop if switching from bold to non-bold

                // if fitting in height of previous span, modify bbox
                if (y0 - y1 <= yTolerance || Math.Abs((y0 - prevHeight) - span.Bbox.Y0) <= yTolerance)
                {
                    span.Bbox = new Rect(span.Bbox.X0, y0 - prevHeight, span.Bbox.X1, y0);
                    spans[i] = span;
                    if (bold)
                        lineBolds[lineBolds.Count - 1] = bold;
                    continue;
                }
                else if (y0 - y1 > 1.5 * prevHeight)
                {
                    break;  // stop if distance to previous line too large
                }

                select.Add(y1);
                lineHeights.Add(height);
                lineBolds.Add(bold);
            }

            if (!select.Any())  // nothing above the table?
                return headerTopRow;

            select = select.Take(5).ToList(); // Only accept up to 5 lines in any header

            // take top row as header if text above table is too far apart
            if (bbox.top - select.First() >= lineHeights.First())
                return headerTopRow;

            // If top row is bold but line above is not, return top row as header
            if (topRowBold && !lineBolds.First())
                return headerTopRow;

            if (!spans.Any())   // nothing left above the table, return top row
                return headerTopRow;

            // Re-compute clip above table
            BBox nclip = new BBox(0,0,0,0);
            foreach (var span in spans.Where(s => s.Bbox.Y1 >= select.Last()))
            {
                nclip = nclip.Union(new BBox(span.Bbox.X0, span.Bbox.Y0, span.Bbox.X1, span.Bbox.Y1));
            }

            if (!nclip.IsEmpty())
                clip = nclip;

            clip.bottom = bbox.bottom;  // make sure we still include every word above

            // Confirm that no word in clip is intersecting a column separator
            List<WordBlock> clipWords = Page.GetTextWords(clip: new Rect(clip.x0, clip.top, clip.x1, clip.bottom));
            List<BBox> wordRects = clipWords.Select(w => new BBox(w.X0, w.Y0, w.X1, w.Y1)).ToList();
            List<float> wordTops = wordRects.Select(r => r.top).Distinct().OrderByDescending(top => top).ToList();

            List<float> wordSelect = new List<float>();

            foreach (var top in wordTops)
            {
                bool intersecting = false;
                foreach (var x in colX)
                {
                    if (x >= 0f)
                    {
                        foreach (var r in wordRects)
                        {
                            // Check if word intersects a column border
                            if (r.top == top && r.x0 < x && r.x1 > x)
                            {
                                intersecting = true;
                                break;
                            }
                        }
                    }
                    if (intersecting)
                    {
                        break;
                    }
                }

                if (!intersecting)
                {
                    wordSelect.Add(top);
                }
                else
                {
                    // Detected a word crossing a column border
                    break;
                }
            }

            if (wordSelect.Count == 0) // nothing left over: return first row
                return headerTopRow;

            BBox hdrBbox = clip;  // compute the header cells
            hdrBbox.top = wordSelect.Last();  // hdr_bbox.top is the smallest top coordinate of words

            List<BBox> hdrCells = new List<BBox>();
            foreach (var c in cells)
            {
                if (c != null)
                {
                    hdrCells.Add(new BBox(c.x0, hdrBbox.top, c.x1, hdrBbox.bottom));
                }
                else
                {
                    hdrCells.Add(null);
                }
            }

            // adjust left/right of header bbox
            hdrBbox.x0 = Bbox.x0;
            hdrBbox.x1 = Bbox.x1;

            // List to store the processed header names
            List<string> hdrNames = new List<string>();

            // Process each header cell
            foreach (var c in hdrCells)
            {
                string cText = Page.GetTextbox(new Rect(c.x0, c.top, c.x1, c.bottom));
                string name = c != null ? cText.Replace("\n", " ").Replace("  ", " ").Trim() : "";
                hdrNames.Add(name);
            }

            return new TableHeader(hdrBbox, hdrCells, hdrNames, true);
        }

        private string ExtractText(List<Dictionary<string, object>> cellChars, Dictionary<string, object> kwargs)
        {
            // Logic to extract text from characters inside a bounding box
            // Placeholder logic
            return string.Join(" ", cellChars.Select(c => c["text"].ToString()));
        }
    }
    public class TableSettings 
    {
        static readonly string[] NON_NEGATIVE_SETTINGS = {
            "snap_tolerance",
            "snap_x_tolerance",
            "snap_y_tolerance",
            "join_tolerance",
            "join_x_tolerance",
            "join_y_tolerance",
            "edge_min_length",
            "min_words_vertical",
            "min_words_horizontal",
            "intersection_tolerance",
            "intersection_x_tolerance",
            "intersection_y_tolerance",
        };

        public string vertical_strategy { get; set; } = "lines";
        public string horizontal_strategy { get; set; } = "lines";
        public List<Edge> explicit_vertical_lines { get; set; } = null;
        public List<Edge> explicit_horizontal_lines { get; set; } = null;
        public float snap_tolerance { get; set; } = TableFlags.TABLE_DEFAULT_SNAP_TOLERANCE;
        public float snap_x_tolerance { get; set; } = TableFlags.TABLE_UNSET;
        public float snap_y_tolerance { get; set; } = TableFlags.TABLE_UNSET;
        public float join_tolerance { get; set; } = TableFlags.TABLE_DEFAULT_JOIN_TOLERANCE;
        public float join_x_tolerance { get; set; } = TableFlags.TABLE_UNSET;
        public float join_y_tolerance { get; set; } = TableFlags.TABLE_UNSET;
        public float edge_min_length { get; set; } = 3.0f;
        public float min_words_vertical { get; set; } = TableFlags.TABLE_DEFAULT_MIN_WORDS_VERTICAL;
        public float min_words_horizontal { get; set; } = TableFlags.TABLE_DEFAULT_MIN_WORDS_HORIZONTAL;
        public float intersection_tolerance { get; set; } = 3.0f;
        public float intersection_x_tolerance { get; set; } = TableFlags.TABLE_UNSET;
        public float intersection_y_tolerance { get; set; } = TableFlags.TABLE_UNSET;
        public Dictionary<string, object> text_settings { get; set; } = null;

        public TableSettings PostInit()
        {
            // Clean up user-provided table settings.
            // Validates that the table settings provided consists of acceptable values and
            // returns a cleaned up version.The cleaned up version fills out the missing
            // values with the default values in the provided settings.
            // TODO: Can be further used to validate that the values are of the correct
            // type.For example, raising a value error when a non-boolean input is
            // provided for the key ``keep_blank_chars``.
            // :param table_settings: User - provided table settings.
            // :returns: A cleaned up version of the user - provided table settings.
            // :raises ValueError: When an unrecognised key is provided.

            foreach (string setting in NON_NEGATIVE_SETTINGS)
            {
                PropertyInfo property = typeof(TableSettings).GetProperty(setting);
                if (property != null)
                {
                    var value = property.GetValue(this);
                    if ((float)value < 0)
                    {
                        throw new ArgumentException("Table setting " + setting + " cannot be negative");
                    }
                }
                else
                {
                    throw new ArgumentException("Table setting not include property " + setting);
                }
            }

            foreach (string orientation in new string[] { "horizontal", "vertical" })
            {
                PropertyInfo property = typeof(TableSettings).GetProperty(orientation + "_strategy");
                if (property != null)
                {
                    var strategy = property.GetValue(this);
                    if (Array.IndexOf(TableFlags.TABLE_STRATEGIES, strategy) == -1)
                    {
                        throw new ArgumentException(orientation + "_strategy  must be one of " + string.Join(",", TableFlags.TABLE_STRATEGIES));
                    }
                }
                else
                {
                    throw new ArgumentException("Table setting not include property " + orientation + "_strategy");
                }
            }

            if (this.text_settings == null)
                this.text_settings = new Dictionary<string, object>();

            // This next section is for backwards compatibility
            foreach (string attr in new string[] { "x_tolerance", "y_tolerance" })
            {
                if (!this.text_settings.ContainsKey(attr))
                {
                    this.text_settings[attr] = this.text_settings.ContainsKey("tolerance") ? this.text_settings["tolerance"] : 3.0f;
                }
            }

            if (this.text_settings.ContainsKey("tolerance"))
            {
                this.text_settings.Remove("tolerance");
            }
            // End of that section

            var mappings = new (string attr, string fallback)[]
            {
                ("snap_x_tolerance", "snap_tolerance"),
                ("snap_y_tolerance", "snap_tolerance"),
                ("join_x_tolerance", "join_tolerance"),
                ("join_y_tolerance", "join_tolerance"),
                ("intersection_x_tolerance", "intersection_tolerance"),
                ("intersection_y_tolerance", "intersection_tolerance")
            };
            foreach (var (attr, fallback) in mappings)
            {
                // Get the property info for the current attribute and fallback
                PropertyInfo attrProperty = typeof(TableSettings).GetProperty(attr);
                PropertyInfo fallbackProperty = typeof(TableSettings).GetProperty(fallback);

                if (attrProperty != null && fallbackProperty != null)
                {
                    float attrValue = (float)attrProperty.GetValue(this);
                    if (attrValue == TableFlags.TABLE_UNSET)
                    {
                        float fallbackValue = (float)fallbackProperty.GetValue(this);
                        attrProperty.SetValue(this, fallbackValue);
                    }
                }
            }

            return this;
        }

        public static TableSettings resolve(object settings = null)
        {
            if (settings == null)
            {
                return new TableSettings();
            }
            else if (settings is TableSettings tableSettings)
            {
                return tableSettings;
            }
            else if (settings is Dictionary<string, object> settingsDict)
            {
                var coreSettings = new Dictionary<string, object>();
                var textSettings = new Dictionary<string, object>();

                // Loop over the dictionary and separate text_ settings
                foreach (var kvp in settingsDict)
                {
                    if (kvp.Key.StartsWith("text_"))
                    {
                        textSettings[kvp.Key.Substring(5)] = kvp.Value.ToString();
                    }
                    else
                    {
                        coreSettings[kvp.Key] = kvp.Value;
                    }
                }

                // Add textSettings to coreSettings before passing to the constructor
                coreSettings["text_settings"] = textSettings;

                var instance = new TableSettings();
                foreach (var kvp in coreSettings)
                {
                    var property = instance.GetType().GetProperty(kvp.Key);

                    if (property != null)
                    {
                        property.SetValue(instance, kvp.Value);
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid parameter: {kvp.Key}");
                    }
                }

                return instance.PostInit();
            }
            else
            {
                throw new ArgumentException($"Cannot resolve settings: {settings}");
            }
        }
    }

    public class TableFinder
    {
        private readonly Page page;
        private readonly TableSettings settings;
        private readonly List<Edge> edges;
        private readonly Dictionary<Point, Intersection> intersections;
        private readonly List<BBox> cells;
        private readonly List<Table> tables;

        private List<Edge> EDGES;
        private List<Character> CHARS;

        public TableFinder(Page page, Rect clip, TableSettings settings = null)
        {
            EDGES = new List<Edge>();
            CHARS = new List<Character>();
            make_chars(page, clip);
            make_edges(page, clip, settings);
            this.page = page;
            this.settings = settings;
            this.edges = get_edges();
            this.intersections = edges_to_intersections(this.edges,
                this.settings.intersection_x_tolerance,
                this.settings.intersection_y_tolerance);
            this.cells = intersections_to_cells(this.intersections);
            this.tables = new List<Table>();

            foreach (var cellGroup in cells_to_tables(this.page, this.cells))
            {
                this.tables.Add(new Table(this.page, cellGroup, CHARS));
            }
        }

        private List<Edge> get_edges()
        {
            var settings = this.settings;

            var strategy = settings.vertical_strategy;
            if (strategy == "explicit") 
            {
                var lines = settings.explicit_vertical_lines;
                if (lines.Count < 2)
                {
                    throw new Exception("If vertical_strategy == 'explicit', " +
                        "explicit_vertical_lines " +
                        "must be specified as a list/tuple of two or more " +
                        "floats/ints.");
                }
            }
            strategy = settings.horizontal_strategy;
            if (strategy == "explicit")
            {
                var lines = settings.explicit_horizontal_lines;
                if (lines.Count < 2)
                {
                    throw new Exception("If horizontal_strategy == 'explicit', " +
                        "explicit_horizontal_lines " +
                        "must be specified as a list/tuple of two or more " +
                        "floats/ints.");
                }
            }

            string v_strat = settings.vertical_strategy;
            string h_strat = settings.horizontal_strategy;

            List<Character> words = new List<Character>();
            if (v_strat == "text" || h_strat == "text")
                words = extract_words(CHARS, settings.text_settings);

            List<Edge> v_explicit = new List<Edge>();
            if (settings.explicit_vertical_lines != null)
            {
                foreach (var desc in settings.explicit_vertical_lines)
                {
                    if (desc is Edge descEdge)
                    {
                        foreach (Edge e in obj_to_edges(descEdge))
                        {
                            if (e.orientation == "v")
                                v_explicit.Add(e);
                        }
                    }
                }
            }

            List<Edge> v_base = new List<Edge>();
            if (v_strat == "lines")
                v_base = filter_edges(EDGES, "v");
            else if (v_strat == "lines_strict")
                v_base = filter_edges(EDGES, "v", edgeType: "lines");
            else if (v_strat == "text")
                v_base = words_to_edges_v(words, wordThreshold:(int)settings.min_words_vertical);
            else if (v_strat == "explicit")
                v_base.Clear();
            else
                v_base.Clear();

            List<Edge> v = v_base.Concat(v_explicit).ToList();

            List<Edge> h_explicit = new List<Edge>();
            if (settings.explicit_horizontal_lines != null)
            {
                foreach (var desc in settings.explicit_horizontal_lines)
                {
                    if (desc is Edge descEdge)
                    {
                        foreach (Edge e in obj_to_edges(descEdge))
                        {
                            if (e.orientation == "h")
                                h_explicit.Add(e);
                        }
                    }
                }
            }

            List<Edge> h_base = new List<Edge>();
            if (h_strat == "lines")
                h_base = filter_edges(EDGES, "h");
            else if (h_strat == "lines_strict")
                h_base = filter_edges(EDGES, "h", edgeType: "lines");
            else if (h_strat == "text")
                h_base = words_to_edges_h(words, wordThreshold:(int)settings.min_words_horizontal);
            else if (h_strat == "explicit")
                h_base.Clear();
            else
                h_base.Clear();

            List<Edge> h = h_base.Concat(h_explicit).ToList();

            List<Edge> edges = new List<Edge>();
            edges.AddRange(v);
            edges.AddRange(h);

            edges = merge_edges(
                edges,
                snap_x_tolerance: settings.snap_x_tolerance,
                snap_y_tolerance: settings.snap_y_tolerance,
                join_x_tolerance: settings.join_x_tolerance,
                join_y_tolerance: settings.join_y_tolerance
                );

            return filter_edges(edges, minLength: settings.edge_min_length);
        }
        public Table this[int i]
        {
            get
            {
                int tcount = this.tables.Count;
                if (i >= tcount || i < 0)
                {
                    throw new IndexOutOfRangeException("table not on page");
                }
                return this.tables[i];
            }
        }

        // Nullify page rotation.
        // To correctly detect tables, page rotation must be zero.
        // This function performs the necessary adjustments and returns information
        // for reverting this changes.
        private static Page page_rotation_set0(Page page)
        {
            Rect mediabox = page.MediaBox;
            int rot = page.Rotation; // contains normalized rotation value
            // need to derotate the page's content
            Rect mb = page.MediaBox;  // current mediabox

            Matrix mat0 = new Matrix();
            if (rot == 90)
            {
                // before derotation, shift content horizontally
                mat0 = new Matrix(1, 0, 0, 1, mb.Y1 - mb.X1 - mb.X0 - mb.Y0, 0);
            }
            else if (rot == 270)
            {
                // before derotation, shift content vertically
                mat0 = new Matrix(1, 0, 0, 1, 0, mb.X1 - mb.Y1 - mb.Y0 - mb.X0);
            }
            else
            {
                mat0 = new Matrix(1, 0, 0, 1, -2 * mb.X0, -2 * mb.Y0);
            }

            // swap x- and y-coordinates
            if (rot == 90 || rot == 270)
            {
                float x0 = mb.X0;
                float y0 = mb.Y0;
                float x1 = mb.X1;
                float y1 = mb.Y1;
                mb.X0 = y0;
                mb.Y0 = x0;
                mb.X1 = y1;
                mb.X1 = x1;
                page.SetMediaBox(mb);
            }

            page.SetRotation(0);

            return page;
        }

        private void make_chars(Page page, Rect clip = null)
        {
            int page_number = page.Number + 1;
            float page_height = page.Rect.Height;
            Matrix ctm = page.TransformationMatrix;
            float doctop_base = page_height * page.Number;

            List<Block> blocks = (page.GetText("rawdict", clip, flags: (int)TextFlagsExtension.TEXTFLAGS_TEXT) as PageInfo).Blocks;

            foreach (var block in blocks)
            {
                foreach (var line in block.Lines)
                {
                    Point ldir = line.Dir;  // = (cosine, sine) of angle
                    ldir = new Point((float)Math.Round(ldir.X, 4), (float)Math.Round(ldir.Y, 4));
                    Matrix matrix = new Matrix(ldir.X, -ldir.Y, ldir.Y, ldir.X, 0, 0);
                    bool upright = ldir.Y == 0f;

                    foreach (var span in line.Spans.OrderBy(s => s.Bbox.X0))
                    {
                        string fontname = span.Font;
                        float fontsize = span.Size;
                        int color = span.Color;

                        foreach (var character in span.Chars.OrderBy(c => c.Bbox.x0))
                        {
                            Rect bbox = new Rect(character.Bbox);
                            Rect bbox_ctm = bbox * ctm;
                            Point origin = new Point(character.Origin) * ctm;

                            matrix.E = origin.X;
                            matrix.F = origin.Y;

                            string text = character.C.ToString();
                            var charDict = new Character();
                            charDict.adv = upright ? bbox.X1 - bbox.X0 : bbox.Y1 - bbox.Y0;
                            charDict.bottom = bbox.Y1;
                            charDict.doctop = bbox.Y0 + doctop_base;
                            charDict.fontname = fontname;
                            charDict.height = bbox.Y1 - bbox.Y0;
                            charDict.matrix = matrix;
                            charDict.ncs = "DeviceRGB";
                            charDict.non_stroking_color = color;
                            charDict.non_stroking_pattern = null;
                            charDict.object_type = "char";
                            charDict.page_number = page_number;
                            charDict.size = upright ? fontsize : bbox.Y1 - bbox.Y0;
                            charDict.stroking_color = color;
                            charDict.stroking_pattern = null;
                            charDict.text = text;
                            charDict.top = bbox.Y0;
                            charDict.upright = upright;
                            charDict.width = bbox.X1 - bbox.X0;
                            charDict.x0 = bbox.X0;
                            charDict.x1 = bbox.X1;
                            charDict.y0 = bbox_ctm.Y0;
                            charDict.y1 = bbox_ctm.Y1;
                            CHARS.Add(charDict);
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------------------
        // Extract all page vector graphics to fill the EDGES list.
        // We are ignoring Bézier curves completely and are converting everything
        // else to lines.
        // ------------------------------------------------------------------------

        private void make_edges(Page page, Rect clip = null, TableSettings tset = null)
        {
            float snap_x = tset.snap_x_tolerance;
            float snap_y = tset.snap_y_tolerance;
            float min_length = tset.edge_min_length;
            TextPage textPage = page.GetTextPage(clip);

            bool linesStrict = tset.vertical_strategy == "lines_strict" || tset.horizontal_strategy == "lines_strict";

            float page_height = page.Rect.Height;
            float doctop_basis = page.Number * page_height;
            int page_number = page.Number + 1;
            Rect prect = page.Rect;

            if (page.Rotation == 90 || page.Rotation == 270)
            {
                float w = prect.BottomRight.X;
                float h = prect.BottomRight.Y;
                prect = new Rect(0, 0, h, w);
            }

            if (clip != null)
                clip = new Rect(clip);
            else
                clip = prect;

            // Detect whether r1, r2 are neighbors.
            // Defined as:
            // The minimum distance between points of r1 and points of r2 is not
            // larger than some delta.
            // This check supports empty rect-likes and thus also lines.
            // Note:
            // This type of check is MUCH faster than native Rect containment checks.
            bool are_neighbors(Rect r1, Rect r2)
            {
                return (    // check if x-coordinates of r1 are within those of r2
                    (r2.X0 - snap_x <= r1.X0 && r1.X0 <= r2.X1 + snap_x) ||
                    (r2.X0 - snap_x <= r1.X1 && r1.X1 <= r2.X1 + snap_x)
                ) && (
                    (r2.Y0 - snap_y <= r1.Y0 && r1.Y0 <= r2.Y1 + snap_y) ||
                    (r2.Y0 - snap_y <= r1.Y1 && r1.Y1 <= r2.Y1 + snap_y)
                ) ||    // same check with r1 / r2 exchanging their roles (this is necessary!)
                (
                    (r1.X0 - snap_x <= r2.X0 && r2.X0 <= r1.X1 + snap_x) ||
                    (r1.X0 - snap_x <= r2.X1 && r2.X1 <= r1.X1 + snap_x)
                ) && (
                    (r1.Y0 - snap_y <= r2.Y0 && r2.Y0 <= r1.Y1 + snap_y) ||
                    (r1.Y0 - snap_y <= r2.Y1 && r2.Y1 <= r1.Y1 + snap_y)
                );
            }

            // Detect and join rectangles of "connected" vector graphics.
            (List<Rect>, List<PathInfo>) clean_graphics()
            {
                // Detect and join rectangles of "connected" vector graphics.
                List<PathInfo> paths = new List<PathInfo>();

                foreach (var p in page.GetDrawings())
                {
                    // ignore fill-only graphics if they do not simulate lines,
                    // which means one of width or height are small.
                    if (p.Type == "f" && linesStrict && p.Rect.Width > snap_x && p.Rect.Height > snap_y)
                    {
                        continue;
                    }
                    paths.Add(p);
                }

                // start with all vector graphics rectangles
                List<Rect> prects = paths.Select(p => p.Rect)
                                         .Distinct()
                                         .OrderBy(r => (r.Y1, r.X0))
                                         .ToList();

                List<BBox> bboxes = new List<BBox>();
                foreach (var p in prects)
                {
                    bboxes.Add(BBox.RectToBBox(p));
                }
                bboxes = bboxes.Distinct().ToList();
                prects.Clear();
                foreach (var b in bboxes)
                {
                    prects.Add(BBox.BBoxToRect(b));
                }

                List<Rect> newRects = new List<Rect>(); // the final list of joined rectangles

                // ----------------------------------------------------------------
                // Strategy: Join rectangles that "almost touch" each other.
                // Extend first rectangle with any other that is a "neighbor".
                // Then move it to the final list and continue with the rest.
                // ----------------------------------------------------------------
                while (prects.Count > 0) // The algorithm will empty this list.
                {
                    Rect prect0 = prects[0]; // Copy of the first rectangle (performance reasons).
                    bool repeat = true;

                    while (repeat) // This loop extends the first rect in the list.
                    {
                        repeat = false; // Set to true again if some other rect touches.

                        for (int i = prects.Count - 1; i > 0; i--) // Run backwards.
                        {
                            if (are_neighbors(prect0, prects[i])) // Close enough to rect 0?
                            {
                                // Extend rect 0.
                                prect0.X0 = Math.Min(prect0.X0, prects[i].X0);
                                prect0.Y0 = Math.Min(prect0.Y0, prects[i].Y0);
                                prect0.X1 = Math.Max(prect0.X1, prects[i].X1);
                                prect0.Y1 = Math.Max(prect0.Y1, prects[i].Y1);

                                prects.RemoveAt(i); // Delete this rect.
                                repeat = true; // Keep checking the rest.
                            }
                        }
                    }

                    // Move rect 0 over to the result list if there is some text in it.
                    if (!string.IsNullOrWhiteSpace(page.GetTextbox(prect0)))
                    {
                        // Contains text, so accept it as a table bbox candidate.
                        newRects.Add(prect0);
                    }

                    prects.RemoveAt(0); // Remove from rect list.
                }

                return (newRects, paths);
            }

            (List<Rect> bboxes, List<PathInfo> paths) = clean_graphics();

            bool IsParallel(Point p1, Point p2)
            {
                if (p1 == null || p2 == null)
                {
                    return false;
                }
                // Check if the line is roughly parallel to either the X or Y axis
                if (Math.Abs(p1.X - p2.X) <= snap_x || Math.Abs(p1.Y - p2.Y) <= snap_y)
                {
                    return true;
                }
                return false;
            }

            // Given 2 points, make a line dictionary for table detection.
            Edge make_line(PathInfo p, Point p1, Point p2, Rect clip)
            {
                if (!IsParallel(p1, p2))  // only accepting axis-parallel lines
                {
                    return null;
                }

                // Compute the extremal values
                float x0 = Math.Min(p1.X, p2.X);
                float x1 = Math.Max(p1.X, p2.X);
                float y0 = Math.Min(p1.Y, p2.Y);
                float y1 = Math.Max(p1.Y, p2.Y);

                // Check for outside clip
                if (x0 > clip.X1 || x1 < clip.X0 || y0 > clip.Y1 || y1 < clip.Y0)
                {
                    return null;
                }

                if (x0 < clip.X0) x0 = clip.X0;  // Adjust to clip boundary
                if (x1 > clip.X1) x1 = clip.X1;  // Adjust to clip boundary
                if (y0 < clip.Y0) y0 = clip.Y0;  // Adjust to clip boundary
                if (y1 > clip.Y1) y1 = clip.Y1;  // Adjust to clip boundary

                float width = x1 - x0;  // From adjusted values
                float height = y1 - y0;  // From adjusted values

                if (width == 0 && height == 0)
                {
                    return null;  // Nothing left to deal with
                }

                Edge line_dict = new Edge();
                line_dict.x0 = x0;
                line_dict.y0 = page_height - y0;
                line_dict.x1 = x1;
                line_dict.y1 = page_height - y1;
                line_dict.width = width;
                line_dict.height = height;
                line_dict.pts = new Point[] { new Point(x0, y0), new Point(x1, y1) };
                line_dict.linewidth = p.Width;
                line_dict.stroke = true;
                line_dict.fill = false;
                line_dict.evenodd = false;
                line_dict.stroking_color = (p.Color != null && p.Color.Length > 0) ? p.Color : p.Fill;
                line_dict.non_stroking_color = null;
                line_dict.object_type = "line";
                line_dict.page_number = page_number;
                line_dict.stroking_pattern = null;
                line_dict.non_stroking_pattern = null;
                line_dict.top = y0;
                line_dict.bottom = y1;
                line_dict.doctop = y0 + doctop_basis;

                return line_dict;
            }

            foreach (PathInfo p in paths)
            {
                List<Item> items = p.Items;  // items in this path

                // if 'closePath', add a line from last to first point
                if (p.ClosePath && items.First().Type == "l" && items.Last().Type == "l")
                {
                    Item line = new Item()
                    {
                        Type = "l",
                        LastPoint = new Point(items.First().P1),
                        P1 = new Point(items.Last().LastPoint)
                    };
                    items.Add(line);
                }

                foreach (Item item in items)
                {
                    if (item.Type != "l" && item.Type != "re" && item.Type != "qu") // ignore anything else
                        continue;

                    if (item.Type == "l")  // a line
                    {
                        var p1 = item.P1;
                        var p2 = item.P2;
                        var lineDict = make_line(p, p1, p2, clip);
                        if (lineDict != null)
                        {
                            EDGES.Add(Global.line_to_edge(lineDict));
                        }
                    }
                    else if (item.Type == "re")
                    {
                        // A rectangle: decompose into 4 lines
                        Rect rect = item.Rect;  // Normalize the rectangle
                        rect.Normalize();

                        // If it simulates a vertical line
                        if (rect.Width <= min_length && rect.Width < rect.Height)
                        {
                            float x = (rect.X1 + rect.X0) / 2;
                            Point p1 = new Point(x, rect.Y0);
                            Point p2 = new Point(x, rect.Y1);
                            var lineDict = make_line(p, p1, p2, clip);
                            if (lineDict != null)
                            {
                                EDGES.Add(line_to_edge(lineDict));
                            }
                            continue;
                        }

                        // If it simulates a horizontal line
                        if (rect.Height <= min_length && rect.Height < rect.Width)
                        {
                            float y = (rect.Y1 + rect.Y0) / 2;
                            var p1 = new Point(rect.X0, y);
                            var p2 = new Point(rect.X1, y);
                            var lineDict = make_line(p, p1, p2, clip);
                            if (lineDict != null)
                            {
                                EDGES.Add(line_to_edge(lineDict));
                            }
                            continue;
                        }

                        var line_dict = make_line(p, rect.TopLeft, rect.BottomLeft, clip);
                        if (line_dict != null)
                            EDGES.Add(line_to_edge(line_dict));
                        line_dict = make_line(p, rect.BottomLeft, rect.BottomRight, clip);
                        if (line_dict != null)
                            EDGES.Add(line_to_edge(line_dict));
                        line_dict = make_line(p, rect.BottomRight, rect.TopRight, clip);
                        if (line_dict != null)
                            EDGES.Add(line_to_edge(line_dict));
                        line_dict = make_line(p, rect.TopRight, rect.TopLeft, clip);
                        if (line_dict != null)
                            EDGES.Add(line_to_edge(line_dict));
                    }
                    else  // must be a quad (quads have 4 points)
                    {
                        Point ul = item.Quad.UpperLeft;
                        Point ur = item.Quad.UpperRight;
                        Point ll = item.Quad.LowerLeft;
                        Point lr = item.Quad.LowerRight;

                        var lineDict = make_line(p, ul, ll, clip);
                        if (lineDict != null)
                        {
                            EDGES.Add(line_to_edge(lineDict));
                        }

                        lineDict = make_line(p, ll, lr, clip);
                        if (lineDict != null)
                        {
                            EDGES.Add(line_to_edge(lineDict));
                        }

                        lineDict = make_line(p, lr, ur, clip);
                        if (lineDict != null)
                        {
                            EDGES.Add(line_to_edge(lineDict));
                        }

                        lineDict = make_line(p, ur, ul, clip);
                        if (lineDict != null)
                        {
                            EDGES.Add(line_to_edge(lineDict));
                        }
                    }
                }
            }

            // Define the path with color, fill, and width
            PathInfo path = new PathInfo();
            path.Color = new float[] { 0f, 0f, 0f };
            path.Fill = null;
            path.Width = 1f;

            foreach (Rect bbox in bboxes)
            {
                var lineDict = make_line(path, bbox.TopLeft, bbox.TopRight, clip);
                if (lineDict != null)
                    EDGES.Add(line_to_edge(lineDict));

                lineDict = make_line(path, bbox.BottomLeft, bbox.BottomRight, clip);
                if (lineDict != null)
                    EDGES.Add(line_to_edge(lineDict));

                lineDict = make_line(path, bbox.TopLeft, bbox.BottomLeft, clip);
                if (lineDict != null)
                    EDGES.Add(line_to_edge(lineDict));

                lineDict = make_line(path, bbox.TopRight, bbox.BottomRight, clip);
                if (lineDict != null)
                    EDGES.Add(line_to_edge(lineDict));
            }

            return;
        }

        public static List<Table> find_tables(
                Page paramPage,
                Rect clip,
                TableSettings tset
            )
        {
            Page page = new Page(paramPage.GetPdfPage(), paramPage.Parent);

            if (page.Rotation != 0)
            {
                page = page_rotation_set0(page);
            }

            TableFinder tableFinder = new TableFinder(page, clip, tset);

            return tableFinder.tables;
        }
    }
}
