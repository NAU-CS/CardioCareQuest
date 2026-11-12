// Unit tests for assets/game-src/shared/ccq_game_kit.tw
// Run with: node tool/test_game_kit.js
// Loads the script passage body, runs it against a tiny fake browser, and
// checks every helper. No dependencies.
"use strict";
const fs = require("fs");
const path = require("path");
const assert = require("assert");

function loadKit(windowStub) {
  const src = fs.readFileSync(
    path.join(__dirname, "..", "assets", "game-src", "shared", "ccq_game_kit.tw"),
    "utf8"
  );
  // Drop the ":: CCQ Game Kit [script]" header line.
  const body = src.split(/\r?\n/).slice(1).join("\n");
  const setup = {};
  const sandbox = { setup, window: windowStub, console };
  new Function("setup", "window", "console", "setTimeout", "clearTimeout",
    "setInterval", "clearInterval", "Date", body)(
    setup, windowStub, console, setTimeout, clearTimeout,
    setInterval, clearInterval, Date);
  return setup.ccq;
}

let passed = 0;
function test(name, fn) {
  try { fn(); passed++; console.log("ok   - " + name); }
  catch (e) { console.error("FAIL - " + name + "\n       " + e.message); process.exitCode = 1; }
}

// ---- bridge ----
test("call returns undefined and does not throw without a bridge", () => {
  const ccq = loadKit({});
  assert.strictEqual(ccq.hasBridge(), false);
  assert.strictEqual(ccq.call("telemetry", "x"), undefined);
});

test("call forwards arguments and returns the result", () => {
  const seen = [];
  const ccq = loadKit({ CCQ: { echo: (a, b) => { seen.push([a, b]); return a + b; } } });
  assert.strictEqual(ccq.call("echo", 2, 3), 5);
  assert.deepStrictEqual(seen, [[2, 3]]);
});

test("call swallows errors thrown by the bridge", () => {
  const ccq = loadKit({ CCQ: { boom: () => { throw new Error("x"); } }, console: { warn() {} } });
  assert.strictEqual(ccq.call("boom"), undefined);
});

// ---- once / submitOnce ----
test("once runs a key only one time", () => {
  const ccq = loadKit({});
  let n = 0;
  assert.strictEqual(ccq.once("a", () => n++), true);
  assert.strictEqual(ccq.once("a", () => n++), false);
  assert.strictEqual(n, 1);
});

test("once releases the key when the function throws", () => {
  const ccq = loadKit({ console: { warn() {} } });
  assert.strictEqual(ccq.once("t", () => { throw new Error("x"); }), false);
  assert.strictEqual(ccq.once("t", () => {}), true);
});

test("resetOnce by prefix lets only matching keys run again", () => {
  const ccq = loadKit({});
  ccq.once("submit:salt", () => {});
  ccq.once("other", () => {});
  ccq.resetOnce("submit:");
  assert.strictEqual(ccq.once("submit:salt", () => {}), true);
  assert.strictEqual(ccq.once("other", () => {}), false);
});

test("submitOnce sends once and keeps the key free when there is no bridge", () => {
  const noBridge = loadKit({});
  assert.strictEqual(noBridge.submitOnce("g", { a: 1 }), false);

  const calls = [];
  const ccq = loadKit({ CCQ: { submitResponse: (a, o) => calls.push([a, o]) } });
  assert.strictEqual(ccq.submitOnce("g", { a: 1 }, { pointsEarned: 50 }), true);
  assert.strictEqual(ccq.submitOnce("g", { a: 1 }, { pointsEarned: 50 }), false);
  assert.deepStrictEqual(calls, [[{ a: 1 }, { pointsEarned: 50 }]]);
});

// ---- lock ----
test("lock blocks a second tap inside the window and allows it after", () => {
  const ccq = loadKit({});
  assert.strictEqual(ccq.lock("k", 50), true);
  assert.strictEqual(ccq.lock("k", 50), false);
  ccq.unlock("k");
  assert.strictEqual(ccq.lock("k", 50), true);
});

// ---- timers ----
test("clearTimers cancels pending timeouts and intervals", (done) => {
  const ccq = loadKit({});
  let fired = 0;
  ccq.after(20, () => fired++);
  ccq.every(10, () => fired++);
  ccq.clearTimers();
  return new Promise((res) => setTimeout(() => { assert.strictEqual(fired, 0); res(); }, 60));
});

test("after still fires when not cleared", () => {
  const ccq = loadKit({});
  let fired = 0;
  ccq.after(5, () => fired++);
  return new Promise((res) => setTimeout(() => { assert.strictEqual(fired, 1); res(); }, 40));
});

// ---- slimVars ----
test("slimVars keeps simple values, truncates text, drops objects", () => {
  const ccq = loadKit({});
  const out = ccq.slimVars({
    day: 3, ok: true, name: "x".repeat(300), order: [1, 2], table: { a: 1 }, nothing: null,
  }, 10);
  assert.deepStrictEqual(out, { day: 3, ok: true, name: "x".repeat(10) });
});

// ---- band ----
test("band picks the first limit the value is below", () => {
  const ccq = loadKit({});
  const t = [[30, "clean"], [60, "middle"], [Infinity, "heavy"]];
  assert.strictEqual(ccq.band(0, t), "clean");
  assert.strictEqual(ccq.band(29, t), "clean");
  assert.strictEqual(ccq.band(30, t), "middle");
  assert.strictEqual(ccq.band(59, t), "middle");
  assert.strictEqual(ccq.band(60, t), "heavy");
  assert.strictEqual(ccq.band(100, t), "heavy");
});

// ---- clamp ----
test("clamp limits values and survives junk input", () => {
  const ccq = loadKit({});
  assert.strictEqual(ccq.clamp(150, 0, 100), 100);
  assert.strictEqual(ccq.clamp(-5, 0, 100), 0);
  assert.strictEqual(ccq.clamp(42, 0, 100), 42);
  assert.strictEqual(ccq.clamp("abc", 0, 100), 0);
  assert.strictEqual(ccq.clamp(NaN, 0, 100), 0);
});

setTimeout(() => console.log(`\n${passed} test(s) passed` + (process.exitCode ? " (with failures)" : "")), 120);
