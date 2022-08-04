﻿namespace TRENZ.Docs.API.Models;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
public class NavTree
{
    public NavTree(Dictionary<string, NavNode> root, bool hasHiddenNodes = false)
    {
        Root = root;
        HasHiddenNodes = hasHiddenNodes;
    }

    public Dictionary<string, NavNode> Root { get; }

    public bool HasHiddenNodes { get; }

    public NavTree WithoutHiddenNodes() => new(FilterChildrenBy(Root, node => node.Order >= 0), HasHiddenNodes);

    public NavTree WithoutChildlessContentlessNodes() => new(FilterChildrenBy(Root, node => node.Children != null || node.HasContent), HasHiddenNodes);

    public NavTree FilterByGroups(IEnumerable<string> groups)
    {
        var groupsList = groups.ToList();

        bool HasPermissionFor(NavNode node, string permission)
        {
            if (!node.Groups.Any())
                return true;

            return node.Groups.Keys
                .Intersect(groupsList)
                .Any(group => node.Groups[group].Contains(permission));
        }

        var childExcluded = false;
        var newRoot = FilterChildrenBy(
            Root,
            node => HasPermissionFor(node, NavNode.PermissionRead),
            node => HasPermissionFor(node, NavNode.PermissionList),
            () => childExcluded = true
        );

        return new(newRoot, childExcluded);
    }

    private static Dictionary<string, NavNode> FilterChildrenBy(
        Dictionary<string, NavNode> subtree,
        Func<NavNode, bool> includeNode,
        Func<NavNode, bool>? includeChildren = null,
        Action? onNodeExcluded = null
    )
    {
        includeChildren ??= _ => true;

        return subtree
            .Where(kvp =>
            {
                var include = includeNode(kvp.Value);
                if (!include)
                    onNodeExcluded?.Invoke();

                return include;
            })
            .Select(tup =>
            {
                if (tup.Value.Children == null)
                    return tup;

                Dictionary<string, NavNode>? children = null;
                var childExcluded = false;
                if (includeChildren(tup.Value))
                    children = FilterChildrenBy(tup.Value.Children, includeNode, includeChildren, () => childExcluded = true);

                return new(
                    tup.Key,
                    new(
                        tup.Value.Location,
                        tup.Value.HasContent
                    )
                    {
                        Children = children is { Count: > 0 } ? children : null,
                        HasHiddenChildren = childExcluded,
                        Groups = tup.Value.Groups,
                    }
                );
            })
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
            );
    }

    public NavNode? FindNodeByLocation(string location) => InternalFindNodeByLocation(location, Root);

    public NavNode? FindNodeByLocationParts(string[] location) => InternalFindNodeByLocation(string.Join(NavNode.Separator, location), Root);

    private static NavNode? InternalFindNodeByLocation(string location, Dictionary<string, NavNode> subtree)
    {
        foreach (var node in subtree.Values)
        {
            if (node.Location.Equals(location, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            if (node.Children is not { Count: > 0 }) continue;
            var child = InternalFindNodeByLocation(location, node.Children);
            if (child != null)
            {
                return child;
            }
        }

        return null;
    }
}
