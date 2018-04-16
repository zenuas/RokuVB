Imports System
Imports System.Reflection
Imports Roku.Manager.SystemLibrary
Imports Roku.Util.Extensions


Namespace Manager

    Public Class RkCILArrayNamespace
        Inherits RkCILNamespace


        Public Overrides Sub CreateFunctionCache()
            MyBase.CreateFunctionCache()

            Dim Item = Me.Functions.FindFirst(Function(x) x.Key.Equals("Item")).Value
            Dim get_Item_index = Item.IndexOf(Function(x) x.Name.Equals("get_Item"))
            Dim set_Item_index = Item.IndexOf(Function(x) x.Name.Equals("set_Item"))
            Dim get_Item = CType(Item(get_Item_index), RkCILFunction)
            Dim set_Item = CType(Item(set_Item_index), RkCILFunction)

            ' get_Item([@T], Int) @T => [get_Item([@T], Int) @T | GetValue([@T], Int) @T]
            Dim get_Item_r As New RkCILArrayFunction With {.Scope = Me, .Name = get_Item.Name, .MethodInfo = get_Item.MethodInfo}
            get_Item_r.Arguments.Add(New NamedValue With {.Name = "self", .Value = Me.BaseType.FixedGeneric(Me.BaseType.Generics.Map(Function(x) get_Item_r.DefineGeneric(x.Name)).ToArray)})
            get_Item_r.Arguments.Add(New NamedValue With {.Name = get_Item.Arguments(1).Name, .Value = Me.Root.LoadType(CType(get_Item.MethodInfo.GetParameters(0).ParameterType, TypeInfo))})
            get_Item_r.Return = get_Item.DefineGeneric(get_Item.MethodInfo.ReturnType.Name)
            get_Item_r.ReplacedFunction = Function(xs) TryLoadFunction(CType(FixedByName(xs(0)), RkCILStruct).FunctionNamespace, "GetValue", xs)
            Item(get_Item_index) = get_Item_r

            ' set_Item([@T], Int, @T) => [set_Item([@T], Int, @T) | SetValue([@T], Int, @T)]
            Dim set_Item_r As New RkCILArrayFunction With {.Scope = Me, .Name = set_Item.Name, .MethodInfo = set_Item.MethodInfo}
            set_Item_r.Arguments.Add(New NamedValue With {.Name = "self", .Value = Me.BaseType.FixedGeneric(Me.BaseType.Generics.Map(Function(x) set_Item_r.DefineGeneric(x.Name)).ToArray)})
            set_Item_r.Arguments.Add(New NamedValue With {.Name = set_Item.Arguments(1).Name, .Value = Me.Root.LoadType(CType(set_Item.MethodInfo.GetParameters(0).ParameterType, TypeInfo))})
            set_Item_r.Arguments.Add(New NamedValue With {.Name = set_Item.Arguments(2).Name, .Value = set_Item_r.DefineGeneric(set_Item.MethodInfo.GetParameters(1).ParameterType.Name)})
            set_Item_r.ReplacedFunction = Function(xs) TryLoadFunction(CType(FixedByName(xs(0)), RkCILStruct).FunctionNamespace, "SetValue", xs)
            Item(set_Item_index) = set_Item_r
        End Sub
    End Class

End Namespace
