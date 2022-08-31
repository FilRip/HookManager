# HookManager
Dyanmicaly replace/decorate method (managed or unmanaged/native) by another one (managed), at runtime. Or decorate : call a method before and after another one.

You can also call older method (replaced) in new method/decorate method

You can also call same method for 2,3,... others methods. You just need to have enough parameters (your replacement/decorate method must have at least the number of parameters of the replaced method how have the most number of parameters)

You can also use Attribute to automaticaly replace/decorate methods at start up<br><br>

Compatible release or debug, x86, x64 or AnyCPU, GAC too (and JIT with some limits).
Compatible with a debugger attached (tested with VisualStudio Debugger)<br><br>

You can also decorate events
You can also replace/decorate Properties
Support Interfaces too<br><br>

You can also replace events (beta, not recommended)<br><br>

Compatible Net Framework 4.x.x

Net 5.0 under work
