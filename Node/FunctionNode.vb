﻿Imports System.Collections.Generic
Imports Roku.Manager
Imports Roku.Util.Extensions


Namespace Node

    Public Class FunctionNode
        Inherits BlockNode
        Implements IHaveScopeType, INamedFunction


        Public Sub New(linenum As Integer)
            MyBase.New(linenum)

        End Sub

        Public Overridable Property Arguments As List(Of DeclareNode)
        Public Overridable Property [Return] As TypeBaseNode
        Public Overridable Property Type As IType Implements IHaveScopeType.Type
        Public Overridable Property Bind As New Dictionary(Of INamedFunction, Boolean) Implements INamedFunction.Bind
        Public Overridable Property Name As String Implements INamedFunction.Name
        Public Overridable ReadOnly Property Where As New List(Of TypeNode)
        Public Overridable Property ImplicitArgumentsCount As UInteger? = Nothing
        Public Overridable Property ImplicitReturn As Boolean = False
        Public Overridable Property Coroutine As Boolean = False Implements INamedFunction.Coroutine

        Public Overridable Property [Function] As RkFunction Implements INamedFunction.Function
            Get
                Return CType(Me.Type, RkFunction)
            End Get
            Set(value As RkFunction)

                Me.Type = value
            End Set
        End Property

        Public Overrides Property Scope As RkScope
            Get
                Return Me.Function
            End Get
            Set(value As RkScope)

                Me.Function = CType(value, RkFunction)
            End Set
        End Property

        Public Overrides Function ToString() As String

            Return $"sub {Me.Name}({String.Join(", ", Me.Arguments.Map(Function(x) x.ToString))}){If(Me.Return Is Nothing, "", $" {Me.Return}")}"
        End Function
    End Class

End Namespace
