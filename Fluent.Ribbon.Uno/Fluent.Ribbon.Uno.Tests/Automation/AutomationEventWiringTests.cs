#nullable enable

namespace FluentUno.Tests.Automation;

using Fluent;
using Fluent.Automation.Peers;
using Fluent.Modern.Automation;
using Fluent.Modern.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

[TestFixture]
public sealed class AutomationEventWiringTests
{
    private const BindingFlags InstanceNonPublic =
        BindingFlags.Instance | BindingFlags.NonPublic;
    private const BindingFlags StaticNonPublic =
        BindingFlags.Static | BindingFlags.NonPublic;

    [Test]
    public void CustomProviderPeersExposeStronglyTypedNotificationMethods()
    {
        var assembly = typeof(RibbonAutomationPeer).Assembly;
        var comboPeer = assembly.GetType(
            "Fluent.Automation.Peers.RibbonComboBoxAccessibleAutomationPeer",
            throwOnError: true)!;

        Assert.Multiple(() =>
        {
            AssertMethod<RibbonAutomationPeer>(
                "RaiseIsMinimizedChanged",
                typeof(bool),
                typeof(bool));
            AssertMethod<RibbonBackstageAutomationPeer>(
                "RaiseIsOpenChanged",
                typeof(bool),
                typeof(bool));
            AssertMethod<RibbonBackstageAutomationPeer>(
                "RaiseChildrenVisibilityChanged",
                typeof(bool));
            AssertMethod<RibbonStartScreenAutomationPeer>(
                "RaiseIsOpenChanged",
                typeof(bool),
                typeof(bool));
            AssertMethod<RibbonStartScreenAutomationPeer>(
                "RaiseChildrenVisibilityChanged",
                typeof(bool));
            AssertMethod<RibbonDropDownButtonAutomationPeer>(
                "RaiseIsDropDownOpenChanged",
                typeof(bool),
                typeof(bool));
            AssertMethod<RibbonGroupBoxAutomationPeer>(
                "RaiseIsDropDownOpenChanged",
                typeof(bool),
                typeof(bool));
            AssertMethod<RibbonInRibbonGalleryAutomationPeer>(
                "RaiseIsDropDownOpenChanged",
                typeof(bool),
                typeof(bool));
            AssertMethod<RibbonTabControlAutomationPeer>(
                "RaiseSelectionChanged",
                typeof(object),
                typeof(object),
                typeof(bool));
            AssertMethod<RibbonTabControlAutomationPeer>(
                "RaiseSelectedTabExpandCollapseChanged",
                typeof(ExpandCollapseState),
                typeof(ExpandCollapseState));
            AssertMethod<RibbonTabItemAutomationPeer>(
                "RaiseIsSelectedChanged",
                typeof(bool),
                typeof(bool));
            AssertMethod<RibbonTabItemAutomationPeer>(
                "RaiseExpandCollapseStateChanged",
                typeof(ExpandCollapseState),
                typeof(ExpandCollapseState));
            AssertMethod<RibbonBackstageTabControlAutomationPeer>(
                "RaiseSelectionChanged",
                typeof(object),
                typeof(object));
            AssertMethod<RibbonStartScreenTabControlAutomationPeer>(
                "RaiseSelectionChanged",
                typeof(BackstageTabItem),
                typeof(BackstageTabItem));
            AssertMethod<RibbonBackstageTabItemAutomationPeer>(
                "RaiseIsSelectedChanged",
                typeof(bool),
                typeof(bool));
            AssertMethod<StatusBarMenuItemAutomationPeer>(
                "RaiseIsCheckedChanged",
                typeof(bool),
                typeof(bool));
            AssertMethod(
                comboPeer,
                "RaiseSelectionChanged",
                typeof(object),
                typeof(int),
                typeof(object),
                typeof(int));
            AssertMethod(
                comboPeer,
                "RaiseValueChanged",
                typeof(string),
                typeof(string));
            AssertMethod<RibbonSearchBoxAutomationPeer>(
                "RaiseValueChanged",
                typeof(string),
                typeof(string));
            Assert.That(
                typeof(IExpandCollapseProvider).IsAssignableFrom(
                    typeof(RibbonTabItemAutomationPeer)),
                Is.True);
        });
    }

    [Test]
    public void StateCallbacksUseExistingElementPeers()
    {
        Assert.Multiple(() =>
        {
            AssertWiring<Ribbon>("OnIsMinimizedChanged", StaticNonPublic, "RaiseIsMinimizedChanged");
            AssertWiring<Backstage>("RaiseIsOpenAutomationEvent", InstanceNonPublic, "RaiseIsOpenChanged");
            AssertWiring<StartScreen>("OnIsOpenChanged", StaticNonPublic, "RaiseIsOpenChanged");
            AssertWiring<RibbonDropDownButton>(
                "OnIsDropDownOpenChanged",
                StaticNonPublic,
                "RaiseIsDropDownOpenChanged");
            AssertWiring<RibbonGroupBox>(
                "OnCompatibilityDropDownChanged",
                StaticNonPublic,
                "RaiseIsDropDownOpenChanged");
            AssertWiring<InRibbonGallery>(
                "OnIsDropDownOpenChanged",
                StaticNonPublic,
                "RaiseIsDropDownOpenChanged");
            AssertWiring<RibbonTabControl>(
                "OnIsDropDownOpenChanged",
                StaticNonPublic,
                "RaiseSelectedTabExpandCollapseChanged");
            AssertWiring<RibbonTabControl>(
                "OnIsMinimizedChanged",
                StaticNonPublic,
                "RaiseSelectedTabExpandCollapseChanged");
            AssertWiring<RibbonTabControl>(
                "OnSelectionChanged",
                InstanceNonPublic,
                "RaiseSelectionChanged");
            AssertWiring<RibbonTabItem>(
                "OnCompatibilitySelectionChanged",
                InstanceNonPublic,
                "RaiseIsSelectedChanged");
            AssertWiring<BackstageTabControl>(
                "OnSelectionChanged",
                InstanceNonPublic,
                "RaiseSelectionChanged");
            AssertWiring<StartScreenTabControl>(
                "SelectTab",
                InstanceNonPublic,
                "RaiseSelectionChanged");
            AssertWiring<BackstageTabItem>(
                "OnIsSelectedChanged",
                StaticNonPublic,
                "RaiseIsSelectedChanged");
            AssertWiring<StatusBarMenuItem>(
                "OnIsCheckedChanged",
                StaticNonPublic,
                "RaiseIsCheckedChanged");
            AssertWiring<RibbonComboBox>(
                "OnAutomationSelectionChanged",
                InstanceNonPublic,
                "RaiseSelectionChanged");
            AssertWiring<RibbonComboBox>(
                "OnAutomationValueChanged",
                InstanceNonPublic,
                "RaiseValueChanged");
            AssertWiring<RibbonSearchBox>(
                "OnTextPropertyChanged",
                StaticNonPublic,
                "RaiseValueChanged");
        });
    }

    private static void AssertMethod<T>(string name, params Type[] parameterTypes)
        => AssertMethod(typeof(T), name, parameterTypes);

    private static void AssertMethod(Type type, string name, params Type[] parameterTypes)
    {
        var method = type.GetMethod(
            name,
            InstanceNonPublic,
            binder: null,
            parameterTypes,
            modifiers: null);
        Assert.That(method, Is.Not.Null, $"{type.Name}.{name}");
    }

    private static void AssertWiring<T>(
        string callbackName,
        BindingFlags bindingFlags,
        string notificationName)
    {
        var callback = typeof(T).GetMethods(bindingFlags | BindingFlags.DeclaredOnly)
            .SingleOrDefault(method => method.Name == callbackName);
        Assert.That(callback, Is.Not.Null, $"{typeof(T).Name}.{callbackName}");

        var referencedMembers = GetReferencedMembers(callback!).ToArray();
        Assert.That(
            referencedMembers.OfType<MethodInfo>().Any(
                method => method.DeclaringType == typeof(FrameworkElementAutomationPeer)
                          && method.Name == nameof(FrameworkElementAutomationPeer.FromElement)),
            Is.True,
            $"{typeof(T).Name}.{callbackName} must use FromElement");
        Assert.That(
            referencedMembers.OfType<MethodInfo>().Any(
                method => method.Name == notificationName),
            Is.True,
            $"{typeof(T).Name}.{callbackName} must call {notificationName}");
    }

    private static IEnumerable<MemberInfo> GetReferencedMembers(MethodInfo method)
    {
        var body = method.GetMethodBody()?.GetILAsByteArray() ?? [];
        for (var offset = 0; offset < body.Length;)
        {
            var opCode = ReadOpCode(body, ref offset);
            if (opCode.OperandType
                is OperandType.InlineField
                or OperandType.InlineMethod
                or OperandType.InlineTok
                or OperandType.InlineType)
            {
                var token = BitConverter.ToInt32(body, offset);
                offset += sizeof(int);
                MemberInfo? member = null;
                try
                {
                    member = method.Module.ResolveMember(
                        token,
                        method.DeclaringType?.GetGenericArguments(),
                        method.GetGenericArguments());
                }
                catch (ArgumentException)
                {
                }

                if (member is not null)
                {
                    yield return member;
                }

                continue;
            }

            offset += GetOperandSize(opCode.OperandType, body, offset);
        }
    }

    private static OpCode ReadOpCode(byte[] body, ref int offset)
    {
        var value = body[offset++];
        if (value != 0xFE)
        {
            return SingleByteOpCodes[value];
        }

        return MultiByteOpCodes[body[offset++]];
    }

    private static int GetOperandSize(
        OperandType operandType,
        byte[] body,
        int offset)
    {
        return operandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget
                or OperandType.ShortInlineI
                or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineBrTarget
                or OperandType.InlineI
                or OperandType.InlineString
                or OperandType.InlineSig => 4,
            OperandType.ShortInlineR => 4,
            OperandType.InlineI8
                or OperandType.InlineR => 8,
            OperandType.InlineSwitch => sizeof(int)
                                        + (BitConverter.ToInt32(body, offset) * sizeof(int)),
            _ => throw new InvalidOperationException(
                $"Unsupported IL operand type {operandType}."),
        };
    }

    private static readonly OpCode[] SingleByteOpCodes = BuildOpCodeMap(multibyte: false);
    private static readonly OpCode[] MultiByteOpCodes = BuildOpCodeMap(multibyte: true);

    private static OpCode[] BuildOpCodeMap(bool multibyte)
    {
        var map = new OpCode[256];
        foreach (var field in typeof(OpCodes).GetFields(
                     BindingFlags.Public | BindingFlags.Static))
        {
            var opCode = (OpCode)field.GetValue(null)!;
            var value = unchecked((ushort)opCode.Value);
            if ((value > byte.MaxValue) == multibyte)
            {
                map[value & byte.MaxValue] = opCode;
            }
        }

        return map;
    }
}
