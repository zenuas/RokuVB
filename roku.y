
%{
Imports Roku.Node
Imports DeclareListNode = Roku.Node.ListNode(Of Roku.Node.DeclareNode)
Imports TypeListNode = Roku.Node.ListNode(Of Roku.Node.TypeBaseNode)
Imports VariableListNode = Roku.Node.ListNode(Of Roku.Node.VariableNode)
Imports IEvaluableListNode = Roku.Node.ListNode(Of Roku.Node.IEvaluableNode)
Imports FunctionListNode = Roku.Node.ListNode(Of Roku.Node.FunctionNode)
%}

%default INode
%define YYROOTNAMESPACE Roku
%define YYNAMESPACE     Parser
%define YYMYNAMESPACE   Parser

%type<BlockNode>      block stmt
%type<IStatementNode> line
%type<LetNode>        let
%type<FunctionNode>   sub sub_block lambda_func cond
%type<FunctionListNode> condn class_block
%type<DeclareNode>    decla lambda_arg
%type<DeclareListNode> args argn lambda_args lambda_argn
%type<TypeNode>       nsvar
%type<TypeBaseNode>   type typev typex atvar union
%type<TypeListNode>   types typen type2n atvarn unionn typeor where nsvarn
%type<IfNode>         if ifthen elseif
%type<SwitchNode>     switch casen case_block
%type<ICaseNode>      case case_expr
%type<StructNode>     struct struct_block
%type<ClassNode>      class
%type<IEvaluableNode> expr cexpr
%type<IEvaluableNode> call
%type<IEvaluableListNode> list listn list2n
%type<VariableListNode>   patternn array_pattern
%type<VariableNode>   var varx fvar fn pattern
%type<NumericNode>    num
%type<StringNode>     str
%type<UseNode>        use
%type<IEvaluableNode> namespace

%left  VAR ATVAR STR NULL TRUE FALSE IF LET SUB
%left  USE
%left  ELSE
%left  ARROW
%left  ':'
%token<NumericNode> NUM
%left  EQ
%left  '?'
%left<TokenNode> ope nope
%left  OPE OR LT GT
%left<TokenNode> or
%left  OR2
%left  AND2
%left<TokenNode> and
%right UNARY
%left  '.'
%left  IGNORE

%left  ','
%left  '(' '[' '{'
%left  EOL

%%

start :                             {$$ = New ProgramNode}
      | program_begin uses stmt END {$$ = Me.PopScope}
program_begin : BEGIN               {Me.PushScope(New ProgramNode)}

uses  : void
      | uses use EOL           {AddUse(Me, $2)}
use   : USE namespace          {$$ = AppendLineNumber(New UseNode With {.Namespace = $2}, $1)}
      | USE var EQ namespace   {$$ = AppendLineNumber(New UseNode With {.Namespace = $4, .Alias = $2.Name}, $1)}

namespace : varx               {$$ = $1}
          | namespace '.' varx {$$ = CreateExpressionNode($1, ".", $3)}


########## statement ##########
stmt  : void        {$$ = Me.CurrentScope}
      | stmt line   {$1.AddStatement($2) : $$ = $1}

line  : call EOL
      | let  EOL
      | sub         {CType(Me.CurrentScope, IAddFunction).AddFunction($1)}
      | if
      | switch
      | block
      | struct      {Me.CurrentScope.Lets.Add($1.Name, $1)}
      | union       {Me.CurrentScope.Lets.Add($1.Name, $1)}
      | class       {Me.CurrentScope.Lets.Add($1.Name, $1)}

block : begin stmt END {$$ = Me.PopScope}
begin : BEGIN          {Me.PushScope(New BlockNode($1.LineNumber))}


########## expr ##########
expr : var
     | str
     | num
     | call
     | lambda
     | atvar
     | '[' list ']'           {$2.AppendLineNumber($1) : $$ = $2}
     | '(' expr ')'           {$$ = CreateExpressionNode($2, "()")}
     | '(' list2n ')'         {$$ = CreateTupleNode($2)}
     | ope expr %prec UNARY   {$$ = CreateFunctionCallNode($1.Token, $2)}
     | expr '.' fvar          {$$ = CreatePropertyNode($1, $2, $3)}
     | expr nope expr         {$$ = CreateFunctionCallNode($2.Token, $1, $3)}
     | expr and  expr         {$$ = CreateIfExpressionNode($1, $3, Nothing)}
     | expr or   expr         {$$ = CreateIfExpressionNode($1, Nothing, $3)}
     | expr '[' expr ']'      {$$ = CreateFunctionCallNode(CreateVariableNode("[]", $2), $1, $3)}
     | expr '?' expr ':' expr {$$ = CreateIfExpressionNode($1, $3, $5)}
     | null
     | bool

call : expr '(' list ')' {$$ = CreateFunctionCallNode($1, $3.List.ToArray)}

list   : void            {$$ = CreateListNode(Of IEvaluableNode)}
       | listn extra
listn  : expr            {$$ = CreateListNode($1)}
       | list2n
list2n : expr ',' expr   {$$ = CreateListNode($1, $3)}
       | list2n ',' expr {$1.List.Add($3) : $$ = $1}

########## let ##########
let : LET var EQ expr          {$$ = CreateLetNode($2, $4, True)}
    | LET var ':' type EQ expr {$$ = CreateLetNode($2, $4, $6, True)}
#    | var EQ expr              {$$ = CreateLetNode($1, $3)}
    | expr '.' varx EQ expr    {$$ = CreateLetNode(CreatePropertyNode($1, $2, $3), $5)}


########## struct ##########
struct : STRUCT var EOL struct_block              {$4.Name = $2.Name : $$ = $4}
       | STRUCT var LT atvarn GT EOL struct_block {$7.Name = $2.Name : $7.Generics.AddRange($4.List) : $$ = $7}

struct_block : struct_begin define END {$$ = Me.PopScope}
struct_begin : BEGIN                   {Me.PushScope(New StructNode($1.LineNumber))}

define : void
       | define LET var ':' type EOL   {Me.CurrentScope.AddLet(CreateLetNode($3, $5))}
       | define LET var EQ  expr EOL   {Me.CurrentScope.AddLet(CreateLetNode($3, $5))}
       | define sub

atvarn : atvar                         {$$ = CreateListNode($1)}
       | atvarn ',' atvar              {$1.List.Add($3) : $$ = $1}


########## union ##########
union  : UNION var EOL BEGIN unionn END {$$ = New UnionNode($2, $5)}

unionn : type EOL        {$$ = CreateListNode($1)}
       | unionn type EOL {$1.List.Add($2) : $$ = $1}


########## class ##########
class : CLASS var LT atvarn GT EOL class_block {$$ = CreateClassNode($2, $4, $7)}

class_block : BEGIN condn END {$$ = $2}

cond  : SUB fn where '(' args        ')' typex EOL {$$ = CreateFunctionNode($2, $5, $7, $3)}
      | SUB fn where '(' typen extra ')' typex EOL {$$ = CreateFunctionNode($2, $5, $8, $3)}
condn : cond                                       {$$ = CreateListNode($1)}
      | condn cond                                 {$1.List.Add($2) : $$ = $1}


########## sub ##########
sub    : SUB fn where '(' args ')' typex EOL sub_block {$$ = CreateFunctionNode($9, $2, $5, $7, $3)}

sub_block : sub_begin stmt END {$$ = Me.PopScope}
sub_begin : BEGIN              {Me.PushScope(New FunctionNode($1.LineNumber))}

fn     : var
       | ope            {$$ = CreateVariableNode($1.Token)}
where  : void
       | LT nsvarn GT   {$$ = $2}
args   : void           {$$ = CreateListNode(Of DeclareNode)}
       | argn extra
argn   : decla          {$$ = CreateListNode($1)}
       | argn ',' decla {$1.List.Add($3) : $$ = $1}
decla  : var ':' type   {$$ = New DeclareNode($1, $3)}
type   : typev
       | typev '?'                {$$ = $1 : $1.Nullable = True}
       | '{' types '}'            {$$ = CreateFunctionTypeNode($2, Nothing, $1)}
       | '{' types ARROW type '}' {$$ = CreateFunctionTypeNode($2, $4,      $1)}
typev  : nsvar
       | '[' type ']'   {$$ = New TypeArrayNode($2)}
       | '[' typeor ']' {$2.AppendLineNumber($1) : $$ = New UnionNode($2)}
       | atvar
       | '[' type2n ']' {$$ = New TypeTupleNode($2)}
nsvarn : nsvar               {$$ = CreateListNode(Of TypeBaseNode)($1)}
       | nsvarn ',' nsvar    {$1.List.Add($3) : $$ = $1}
nsvar  : varx                {$$ = New TypeNode($1)}
       | nsvar '.' varx      {$$ = New TypeNode($1, $3)}
       | nsvar '(' typen ')' {$1.Arguments = $3.List : $$ = $1}
typex  : void
       | type
types  : void           {$$ = CreateListNode(Of TypeBaseNode)()}
       | typen extra
typen  : type           {$$ = CreateListNode($1)}
       | typen ',' type {$1.List.Add($3) : $$ = $1}
type2n : type ',' typen {$3.List.Insert(0, $1) : $$ = $3}
typeor : type OR type   {$$ = CreateListNode($1, $3)}
       | typeor OR type {$1.List.Add($3) : $$ = $1}


########## lambda ##########
lambda      : '{' lambda_args               ARROW lambda_func '}' {$$ = CreateImplicitLambdaFunction(Me.CurrentScope, $4, $2, Nothing)}
            | '{' '(' lambda_args ')' typex ARROW lambda_func '}' {$$ = CreateLambdaFunction(Me.CurrentScope, $7, $3, $5)}
            |                               ARROW lambda_func     {$$ = CreateImplicitLambdaFunction(Me.CurrentScope, $2, Nothing, Nothing)}
lambda_func : expr                       {$$ = ToLambdaExpression(Me.CurrentScope, $1)}
            | EOL sub_block              {$$ = $2}
lambda_arg  : var                        {$$ = New DeclareNode($1, Nothing)}
            | decla
lambda_args : void                       {$$ = CreateListNode(Of DeclareNode)()}
            | lambda_argn extra
lambda_argn : lambda_arg                 {$$ = CreateListNode($1)}
            | lambda_argn ',' lambda_arg {$1.List.Add($3) : $$ = $1}


########## if ##########
if     : ifthen
       | elseif
       | ifthen ELSE EOL block {$$ = AddElse($1, $4)}
       | elseif ELSE EOL block {$$ = AddElse($1, $4)}
ifthen : IF expr EOL block                 {$$ = CreateIfNode($2, $4)}
       | IF var ':' type EQ expr EOL block {$$ = CreateIfCastNode($2, $4, $6, $8)}
elseif : ifthen ELSE ifthen    {$$ = AddElse($1, ToBlock(Me.CurrentScope, $3))}
       | elseif ELSE ifthen    {$$ = AddElse($1, ToBlock(Me.CurrentScope, $3))}


########## switch ##########
switch     : SWITCH expr EOL case_block {$$ = $4 : $4.Expression = $2 : $4.AppendLineNumber($1)}
case_block : BEGIN casen END           {$$ = $2}
casen      : case                      {$$ = CreateSwitchNode($1)}
           | casen case                {$$ = $1 : $1.Case.Add($2)}
case       : case_expr ARROW EOL       {$$ = $1}
           | case_expr ARROW EOL block {$$ = $1 : $1.Then = $4}
           | case_expr ARROW expr EOL  {$$ = $1 : $1.Then = ToLambdaExpressionBlock(Me.CurrentScope, $3)}
case_expr  : cexpr                  {$$ = CreateCaseValueNode(ToBlock(Me.CurrentScope, $1))}
           | var ':' type           {$$ = CreateCaseCastNode($3, $1)}
           | '[' array_pattern ']'  {$$ = CreateCaseArrayNode($2, $1)}
#           | '(' tupple_pattern ')' {}

cexpr : var
      | str
      | num
      | cexpr '(' list ')'      {$$ = CreateFunctionCallNode($1, $3.List.ToArray)}
      | '(' expr ')'            {$$ = CreateExpressionNode($2, "()")}
      | ope expr %prec UNARY    {$$ = CreateFunctionCallNode($1.Token, $2)}
      | cexpr '.' fvar          {$$ = CreatePropertyNode($1, $2, $3)}
      | cexpr nope expr         {$$ = CreateFunctionCallNode($2.Token, $1, $3)}
      | cexpr and  expr         {$$ = CreateFunctionCallNode($2.Token, $1, $3)}
      | cexpr or   expr         {$$ = CreateFunctionCallNode($2.Token, $1, $3)}
      | cexpr '[' expr ']'      {$$ = CreateFunctionCallNode(CreateVariableNode("[]", $2), $1, $3)}
      | cexpr '?' expr ':' expr {$$ = CreateIfExpressionNode($1, $3, $5)}
      | null
      | bool

array_pattern  : patterns
tupple_pattern : patterns
patterns       : void                 {$$ = CreateListNode(Of VariableNode)()}
               | patternn extra
patternn       : pattern              {$$ = CreateListNode($1)}
               | patternn ',' pattern {$1.List.Add($3) : $$ = $1}
pattern        : var

########## other ##########
var    : VAR         {$$ = CreateVariableNode($1)}
       | '(' ope ')' {$$ = CreateVariableNode($2.Token)}
varx   : var
       | SUB     {$$ = CreateVariableNode($1)}
       | IF      {$$ = CreateVariableNode($1)}
       | ELSE    {$$ = CreateVariableNode($1)}
       | LET     {$$ = CreateVariableNode($1)}
       | USE     {$$ = CreateVariableNode($1)}
fvar   : varx
       | NUM     {$$ = CreateVariableNode($1.Format, $1)}
atvar  : ATVAR   {$$ = New TypeNode(CreateVariableNode($1)) With {.IsGeneric = True}}
num    : NUM     {$$ = $1}
str    : STR     {$$ = New StringNode($1)}
       | str STR {$1.String.Append($2.Name) : $$ = $1}
null   : NULL    {$$ = New NullNode($1)}
bool   : TRUE    {$$ = New BoolNode($1, True)}
       | FALSE   {$$ = New BoolNode($1, False)}
ope    : nope
       | and
       | or
nope   : OPE     {$$ = New TokenNode($1)}
       | OR      {$$ = New TokenNode($1)}
       | LT      {$$ = New TokenNode($1)}
       | GT      {$$ = New TokenNode($1)}
and    : AND2    {$$ = New TokenNode($1)}
or     : OR2     {$$ = New TokenNode($1)}

extra  : void
       | ','

void   : {$$ = Nothing}

