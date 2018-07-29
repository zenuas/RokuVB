Imports System.Collections.Generic


Namespace Manager

    Public Class RkScope
        Implements IScope

        Public Overridable Property Name As String = "" Implements IScope.Name
        Public Overridable Property Parent As IScope Implements IScope.Parent
        Public Overridable ReadOnly Property Structs As New Dictionary(Of String, List(Of IStruct)) Implements IScope.Structs
        Public Overridable ReadOnly Property Classes As New Dictionary(Of String, List(Of RkClass)) Implements IScope.Classes
        Public Overridable ReadOnly Property Functions As New Dictionary(Of String, List(Of IFunction)) Implements IScope.Functions
        Public Overridable Property Closure As RkStruct Implements IClosure.Closure
        Public Overridable ReadOnly Property InnerScopes As New List(Of IScope) Implements IScope.InnerScopes

        Public Overridable Sub AddStruct(x As IStruct) Implements IScope.AddStruct

            Me.AddStruct(x, x.Name)
        End Sub

        Public Overridable Sub AddStruct(x As IStruct, name As String) Implements IScope.AddStruct

            If Not Me.Structs.ContainsKey(name) Then Me.Structs.Add(name, New List(Of IStruct))
            Me.Structs(name).Add(x)
        End Sub

        Public Overridable Sub AddClass(x As RkClass) Implements IScope.AddClass

            Me.AddClass(x, x.Name)
        End Sub

        Public Overridable Sub AddClass(x As RkClass, name As String) Implements IScope.AddClass

            If Not Me.Classes.ContainsKey(name) Then Me.Classes.Add(name, New List(Of RkClass))
            Me.Classes(name).Add(x)
        End Sub

        Public Overridable Iterator Function FindCurrentStruct(name As String) As IEnumerable(Of IStruct) Implements IScope.FindCurrentStruct

            If Me.Structs.ContainsKey(name) Then

                For Each s In Me.Structs(name)

                    Yield s
                Next
            End If
        End Function

        Public Overridable Sub AddFunction(x As IFunction) Implements IScope.AddFunction

            Me.AddFunction(x, x.Name)
        End Sub

        Public Overridable Sub AddFunction(x As IFunction, name As String) Implements IScope.AddFunction

            If Not Me.Functions.ContainsKey(name) Then Me.Functions.Add(name, New List(Of IFunction))
            Me.Functions(name).Add(x)
        End Sub

        Public Overridable Sub AddInnerScope(x As IScope) Implements IScope.AddInnerScope

            If Not Me.InnerScopes.Contains(x) Then Me.InnerScopes.Add(x)
        End Sub

        Public Overridable Iterator Function FindCurrentFunction(name As String) As IEnumerable(Of IFunction) Implements IScope.FindCurrentFunction

            If Me.Functions.ContainsKey(name) Then

                For Each f In Me.Functions(name)

                    Yield f
                Next
            End If
        End Function

    End Class

End Namespace
