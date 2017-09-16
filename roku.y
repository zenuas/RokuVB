
%{
Imports Roku.Node
Imports DeclareListNode = Roku.Node.ListNode(Of Roku.Node.DeclareNode)
Imports TypeListNode = Roku.Node.ListNode(Of Roku.Node.TypeNode)
Imports IEvaluableListNode = Roku.Node.ListNode(Of Roku.Node.IEvaluableNode)
%}

%default INode
%define YYROOTNAMESPACE Roku
%define YYNAMESPACE     Parser
%define YYMYNAMESPACE   Parser

%type<BlockNode>      block stmt lambda_func
%type<IEvaluableNode> line
%type<LetNode>        let
%type<FunctionNode>   sub
%type<DeclareNode>    decla lambda_arg
%type<DeclareListNode> args argn lambda_args lambda_argn
%type<TypeNode>       type typex atvar
%type<TypeListNode>   types typen atvarn
%type<IfNode>         if ifthen elseif
%type<StructNode>     struct struct_block
%type<IEvaluableNode> expr
%type<IEvaluableNode> call
%type<IEvaluableListNode> list listn
%type<VariableNode>   var varx fn
%type<NumericNode>    num
%type<StringNode>     str
%type<UseNode>        use
%type<IEvaluableNode> namespace

%left  VAR ATVAR STR NULL
%left  USE
%left  ELSE
%token<NumericNode> NUM
%left  OPE
%left  '.'
%left  ALLOW

%left  '?'
%right '(' '[' '{'

%%

start :                             {$$ = New ProgramNode}
      | program_begin uses stmt END {$$ = Me.PopScope}
program_begin : BEGIN               {Me.PushScope(New ProgramNode)}

uses  : void
      | uses use EOL           {Me.AddUse($2)}
use   : USE namespace          {$$ = Me.AppendLineNumber(New UseNode With {.Namespace = $2}, $1)}
      | USE var EQ namespace   {$$ = Me.AppendLineNumber(New UseNode With {.Namespace = $4, .Alias = $2.Name}, $1)}

namespace : varx               {$$ = $1}
          | namespace '.' varx {$$ = Me.CreateExpressionNode($1, ".", $3)}


########## statement ##########
stmt  : void        {$$ = Me.CurrentScope}
      | stmt line   {$1.AddStatement($2) : $$ = $1}

line  : expr EOL
      | let  EOL
      | sub         {CType(Me.CurrentScope, IAddFunction).AddFunction($1)}
      | if
      | switch
      | block
      | struct      {Me.CurrentScope.Scope.Add($1.Name, $1)}

block : begin stmt END {$$ = Me.PopScope}
begin : BEGIN          {Me.PushScope(New BlockNode($1.LineNumber.Value))}


########## expr ##########
expr : var
     | str
     | num
     | call
     | lambda
     | atvar
     | '[' list ']'      {$$ = $2}
     | '(' expr ')'      {$$ = Me.CreateExpressionNode($2, "()")}
#     | OPE expr          {$$ = Me.CreateFunctionCallNode($1, $2)}
     | expr '.' varx     {$$ = New PropertyNode With {.Left = $1, .Right = $3}}
     | expr OPE expr     {$$ = Me.CreateFunctionCallNode($2, $1, $3)}
     | expr '[' expr ']' {$$ = Me.CreateFunctionCallNode(Me.CreateVariableNode("[]", $2), $1, $3)}
     | expr '?' expr ':' expr
     | null

call : expr '(' list ')' {$$ = Me.CreateFunctionCallNode($1, $3.List.ToArray)}

list  : void             {$$ = Me.CreateListNode(Of IEvaluableNode)}
      | listn extra
listn : expr             {$$ = Me.CreateListNode($1)}
      | listn ',' expr   {$1.List.Add($3) : $$ = $1}


########## let ##########
let : LET var EQ expr       {$$ = Me.CreateLetNode($2, $4, True)}
    | var EQ expr           {$$ = Me.CreateLetNode($1, $3, False)}
    | expr '.' varx EQ expr {$$ = Me.CreateLetNode(New PropertyNode With {.Left = $1, .Right = $3}, $5)}


########## struct ##########
struct : STRUCT var EOL struct_block                {$4.Name = $2.Name : $$ = $4}
       | STRUCT var '(' atvarn ')' EOL struct_block {$7.Name = $2.Name : $7.Generics.AddRange($4.List) : $$ = $7}

struct_block : struct_begin define END {$$ = Me.PopScope}
struct_begin : BEGIN                   {Me.PushScope(New StructNode($1.LineNumber.Value))}

define : void
       | define LET var ':' type EOL   {Me.CurrentScope.AddLet(Me.CreateLetNode($3, $5))}
       | define LET var EQ  expr EOL   {Me.CurrentScope.AddLet(Me.CreateLetNode($3, $5, True))}
       | define sub

atvarn : atvar                         {$$ = Me.CreateListNode($1)}
       | atvarn ',' atvar              {$1.List.Add($3) : $$ = $1}


########## sub ##########
sub   : SUB fn '(' args ')' typex EOL block {$$ = Me.CreateFunctionNode($2, $4.List.ToArray, $6, $8)}
fn    : var
      | OPE            {$$ = Me.CreateVariableNode($1)}
args  : void           {$$ = Me.CreateListNode(Of DeclareNode)}
      | argn extra
argn  : decla          {$$ = Me.CreateListNode($1)}
      | argn ',' decla {$1.List.Add($3) : $$ = $1}
decla : var ':' type   {$$ = New DeclareNode($1, $3)}
type  : var            {$$ = New TypeNode($1)}
      | var '?'        {$$ = New TypeNode($1) With {.Nullable = True}}
      | '[' type ']'   {$$ = New TypeArrayNode($2)}
      | atvar
      | atvar '?'      {$$ = $1 : $1.Nullable = True}
      | '{' types '}'            {$$ = CreateFunctionTypeNode($2.List.ToArray, Nothing, $1)}
      | '{' types '}' ALLOW type {$$ = CreateFunctionTypeNode($2.List.ToArray, $5,      $1)}
typex : void
      | type
types : void           {$$ = Me.CreateListNode(Of TypeNode)}
      | typen extra
typen : type           {$$ = Me.CreateListNode($1)}
      | typen ',' type {$1.List.Add($3) : $$ = $1}


########## lambda ##########
lambda      : '{' lambda_args '}' typex ALLOW lambda_func {$$ = Me.CreateFunctionNode($2.List.ToArray, $4, $6)}
lambda_func : expr                       {$$ = Me.ToBlock($1)}
            | block
lambda_arg  : var                        {$$ = New DeclareNode($1, Nothing)}
            | decla
lambda_args : void                       {$$ = Me.CreateListNode(Of DeclareNode)}
            | lambda_argn extra
lambda_argn : lambda_arg                 {$$ = Me.CreateListNode($1)}
            | lambda_argn ',' lambda_arg {$1.List.Add($3) : $$ = $1}


########## if ##########
if     : ifthen
       | elseif
       | ifthen ELSE EOL block {$1.Else = $4 : $$ = $1}
       | elseif ELSE EOL block {$1.Else = $4 : $$ = $1}
ifthen : IF expr EOL block     {$$ = Me.CreateIfNode($2, $4)}
elseif : ifthen ELSE ifthen    {$1.Else = Me.ToBlock($3) : $$ = $1}
       | elseif ELSE ifthen    {$1.Else = Me.ToBlock($3) : $$ = $1}


########## switch ##########
switch     : SWITCH expr EOL case_block
case_block : BEGIN casen END
casen      : case
           | casen case
case       : case_expr ':' EOL
           | case_expr ':' EOL block
           | case_expr ':' expr EOL
case_expr  : var
           | num
           | str
           | '[' array_pattern ']'
           | '(' tupple_pattern ')'

array_pattern  : patterns
tupple_pattern : patterns
patterns       : void
               | patternn extra
patternn       : pattern
               | patternn ',' pattern
pattern        : var

########## other ##########
var   : VAR     {$$ = Me.CreateVariableNode($1)}
varx  : var
      | SUB     {$$ = Me.CreateVariableNode($1)}
      | IF      {$$ = Me.CreateVariableNode($1)}
      | ELSE    {$$ = Me.CreateVariableNode($1)}
      | LET     {$$ = Me.CreateVariableNode($1)}
      | USE     {$$ = Me.CreateVariableNode($1)}
atvar : ATVAR   {$$ = New TypeNode(Me.CreateVariableNode($1)) With {.IsGeneric = True}}
num   : NUM     {$$ = $1}
str   : STR     {$$ = New StringNode($1)}
      | str STR {$1.String.Append($2.Name) : $$ = $1}
null  : NULL    {$$ = New NullNode($1)}

extra : void
      | ','

void  : {$$ = Nothing}

