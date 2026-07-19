# Changelog

All notable changes to Compze.DocumentDb will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.1.0-alpha

- Session affinity is transactional, never thread-bound: `DocumentDbSession` keeps its `SingleTransactionUsageGuard` (one session must never serve two transactions) and sheds the thread-affinity half of its guard combination — an async unit of work legitimately migrates across pool threads, and the session moves with its transaction, which flows across awaits.
- Initial pre-release
