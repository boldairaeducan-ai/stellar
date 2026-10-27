# Balance simulator

Plays Stellar Idle with the real game rules (`Assets/Scripts/Core`) and the real
`GameBalance.json`, to check pacing after any balance change.

```bash
cd Tools/BalanceSim
dotnet run -c Release -- industry 60    # branch (industry | command | logistics), days
```

Simulated player: 2 h of active play per day (2 taps/s), offline the rest, greedy
building/research purchases, warps when Dark Matter would grow by 40% (or after 6 h),
spends Dark Matter in one tree branch, crunches as soon as possible.

Each line is one warp: day, active hours, warp number, galaxy, credits earned in the
run, Dark Matter gained / earned this cycle / in the bank, tree tiers unlocked, Singularities.
