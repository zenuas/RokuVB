﻿Imports System
Imports System.Collections.Generic


Namespace Manager

    Public Class RkGenericEntry
        Implements IType

        Public Overridable Property Name As String Implements IType.Name
        Public Overridable Property Reference As IType = Nothing

        Public Overridable Function GetValue(name As String) As IType Implements IType.GetValue

            Throw New NotImplementedException()
        End Function

        Public Overridable Function DefineGeneric(name As String) As RkGenericEntry Implements IType.DefineGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As IType) As IType Implements IType.FixedGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function FixedGeneric(ParamArray values() As NamedValue) As IType Implements IType.FixedGeneric

            Return Util.Functions.Car(Util.Functions.Where(values, Function(x) x.Name.Equals(Me.Name))).Value
        End Function

        Public Overridable Function HasGeneric() As Boolean Implements IType.HasGeneric

            Throw New NotImplementedException()
        End Function

        Public Overridable Function CloneGeneric() As IType Implements IType.CloneGeneric

            Throw New NotImplementedException()
        End Function
    End Class

End Namespace