// Mirrors Compze.Threading's global usings for the shared assertion facility so the extracted
// cross-process code keeps using the bare Argument/State/... contract statics, exactly as it did in-package.
global using static Compze.Contracts.Contract;
