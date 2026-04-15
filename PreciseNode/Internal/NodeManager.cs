using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/******************************************************************************
 * Copyright (c) 2013-2014, Justin Bengtson
 * Copyright (c) 2014-2016, Maik Schreiber
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met: 
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 ******************************************************************************/

namespace RegexKSP {
	internal class NodeManager {
		internal ManeuverNode node;
		internal ManeuverNode nextNode;
		internal bool encounter;
		internal bool resizeMainWindow;
		internal bool resizeClockWindow;

		internal bool progradeParsed = true;
		internal bool radialParsed = true;
		internal bool normalParsed = true;
		internal bool timeParsed = true;
		internal string progradeText = "";
		internal string radialText = "";
		internal string normalText = "";
		internal string timeText = "";

		internal int angleRefIndex = 0; // 0=Periapsis, 1=AN, 2=VernalEquinox
		internal string angleText = "0";
		internal bool angleParsed = true;

		internal bool HasMemorized {
			get {
				return memory != null;
			}
		}

		private NodeState curNodeState;
		private NodeState curState;
		private bool changed;
		private NodeState memory;

		internal NodeManager() {
			curState = new NodeState();
		}

		internal NodeManager(ManeuverNode n) {
			curState = new NodeState(n);
			curNodeState = new NodeState();
			node = n;
			updateCurrentNodeState();
			updateAngleText();

			if (n.findNextEncounter() != null) {
				encounter = true;
			}
		}

		internal NodeManager nextState() {
			if (nextNode != null) {
				NodeManager next = new NodeManager(nextNode);
				next.angleRefIndex = angleRefIndex;
				next.updateAngleText();
				return next;
			}
			if (node.findNextEncounter() != null) {
				encounter = true;
			}
			return this;
		}

		private void setPrograde(double d) {
			if (d != curState.deltaV.z) {
				curState.deltaV.z = d;
				progradeText = curState.deltaV.z.ToString();
				changed = true;
			}
		}

		internal void addPrograde(double d) {
			setPrograde(curState.deltaV.z + d);
		}

		internal void setPrograde(String s) {
			if (!s.Equals(progradeText, StringComparison.Ordinal)) {
				progradeText = s;
				if (s.EndsWith(".")) {
					progradeParsed = false;
					return;
				}
				double d;
				progradeParsed = double.TryParse(progradeText, out d);
				if (progradeParsed) {
					setPrograde(d);
				}
			}
		}

		private void setNormal(double d) {
			if (d != curState.deltaV.y) {
				curState.deltaV.y = d;
				normalText = curState.deltaV.y.ToString();
				changed = true;
			}
		}

		internal void addNormal(double d) {
			setNormal(curState.deltaV.y + d);
		}

		internal void setNormal(String s) {
			if (!s.Equals(normalText, StringComparison.Ordinal)) {
				normalText = s;
				if (s.EndsWith(".")) {
					normalParsed = false;
					return;
				}
				double d;
				normalParsed = double.TryParse(normalText, out d);
				if (normalParsed) {
					setNormal(d);
				}
			}
		}

		private void setRadial(double d) {
			if (d != curState.deltaV.x) {
				curState.deltaV.x = d;
				radialText = curState.deltaV.x.ToString();
				changed = true;
			}
		}

		internal void addRadial(double d) {
			setRadial(curState.deltaV.x + d);
		}

		internal void setRadial(String s) {
			if (!s.Equals(radialText, StringComparison.Ordinal)) {
				radialText = s;
				if (s.EndsWith(".")) {
					radialParsed = false;
					return;
				}
				double d;
				radialParsed = double.TryParse(radialText, out d);
				if (radialParsed) {
					setRadial(d);
				}
			}
		}

		internal void memorize() {
			memory = (NodeState) curState.Clone();
		}

		internal void clearMemory() {
			memory = null;
		}

		internal void recallMemory() {
			setUT(memory.UT);
			setPrograde(memory.deltaV.z);
			setNormal(memory.deltaV.y);
			setRadial(memory.deltaV.x);
		}

		internal double currentUT() {
			return curState.UT;
		}

		internal void addUT(double d) {
			setUTInternal(curState.UT + d);
			updateAngleText();
		}

		internal void setUT(double d) {
			setUTInternal(d);
			updateAngleText();
		}

		// Sets UT without syncing angleText. Use when the caller manages angleText itself.
		private void setUTInternal(double d) {
			curState.UT = d;
			timeText = curState.UT.ToString();
			changed = true;
		}

		internal void setUT(String s) {
			if (timeText.Equals(s, StringComparison.Ordinal)) {
				return;
			}
			double d;
			timeText = s;
			if (s.EndsWith(".")) {
				timeParsed = false;
				return;
			}
			timeParsed = double.TryParse(timeText, out d);
			if (timeParsed) {
				if (d != curState.UT) {
					timeText = d.ToString();
					curState.UT = d;
					changed = true;
					updateAngleText();  // UT came from external text input, sync angle display
				}
			}
		}

		internal double currentMagnitude() {
			return curState.deltaV.magnitude;
		}

		internal void setPeriapsis() {
			setUT(Planetarium.GetUniversalTime() + node.patch.timeToPe);
		}

		internal void setApoapsis() {
			setUT(Planetarium.GetUniversalTime() + node.patch.timeToAp);
		}

		private double computeAngleFromUT(double ut) {
			if (node == null || node.patch == null) return 0.0;
			double taDeg = node.patch.TrueAnomalyAtUT(ut) * (180.0 / Math.PI);
			// KSP stores argumentOfPeriapsis and LAN in degrees, matching taDeg units here.
			switch (angleRefIndex) {
				case 1:  return (taDeg + node.patch.argumentOfPeriapsis).Angle360();
				case 2:  return (taDeg + node.patch.argumentOfPeriapsis + node.patch.LAN).Angle360();
				default: return taDeg.Angle360();
			}
		}

		private double computeUTFromAngle(double angleDeg) {
			if (node == null || node.patch == null) return curState.UT;
			double taDeg;
			switch (angleRefIndex) {
				case 1:  taDeg = angleDeg - node.patch.argumentOfPeriapsis; break;
				case 2:  taDeg = angleDeg - node.patch.argumentOfPeriapsis - node.patch.LAN; break;
				default: taDeg = angleDeg; break;
			}
			// Get the base near-future occurrence, then shift by period to find the
			// occurrence closest to the node's current UT.
			double ut = node.patch.GetUTforTrueAnomaly(taDeg.Angle360() * Math.PI / 180.0, 2);
			if (node.patch.isClosed()) {
				double p = node.patch.period;
				while (ut < curState.UT - p / 2.0) ut += p;
				while (ut > curState.UT + p / 2.0) ut -= p;
			}
			return node.patch.isUTInsidePatch(ut) ? ut : curState.UT;
		}

		internal void updateAngleText() {
			if (node == null || node.patch == null) return;
			angleText   = Math.Round(computeAngleFromUT(curState.UT), 4).ToString("0.####");
			angleParsed = true;
		}

		internal void setAngleRef(int refIndex) {
			angleRefIndex = refIndex;
			updateAngleText();
		}

		internal void setAngle(string s) {
			if (angleText.Equals(s, StringComparison.Ordinal)) return;
			angleText = s;
			if (s.EndsWith(".")) { angleParsed = false; return; }
			double d;
			angleParsed = double.TryParse(angleText, out d);
			if (angleParsed) {
				// Use setUTInternal so updateAngleText is not called and the user's
				// exact typed string is preserved in angleText without round-tripping.
				setUTInternal(computeUTFromAngle(d));
			}
		}

		internal void addAngle(double d) {
			if (node == null || node.patch == null) return;
			double newAngle = (computeAngleFromUT(curState.UT) + d).Angle360();
			setUTInternal(computeUTFromAngle(newAngle));
			// Display the pre-round-trip value so the field doesn't drift with repeated presses.
			angleText = Math.Round(newAngle, 4).ToString("0.####");
		}

		internal bool hasNode() {
			if (node == null) {
				return false;
			}
			return true;
		}

		internal void updateNode() {
			// Node manager policy:
			// if the manager has been changed from the last update manager snapshot, take the manager
			// UNLESS
			// if the node has been changed from the last update node snapshot, take the node
			if (curNodeState.compare(node)) {
				// the node hasn't changed, do our own thing
				if (changed) {
					if (node.attachedGizmo != null) {
						node.attachedGizmo.DeltaV = curState.getVector();
						node.attachedGizmo.UT = curState.UT;
					}
					node.OnGizmoUpdated(curState.getVector(), curState.UT);
					updateCurrentNodeState();
					changed = false; // new
				}
			} else {
				// the node has changed, take the node's new information for ourselves.
				updateCurrentNodeState();
				curState.update(node);
				updateAngleText();
			}
		}

		private void updateCurrentNodeState() {
			curNodeState.update(node);
			progradeText = node.DeltaV.z.ToString();
			normalText = node.DeltaV.y.ToString();
			radialText = node.DeltaV.x.ToString();
			timeText = node.UT.ToString();
		}
	}
}
