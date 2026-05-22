# Training a Sumo Champion: Applied Reinforcement Learning
Unity ML-Agents | C# | Proximal Policy Optimization (PPO)

An applied deep reinforcement learning project focused on training a 3D ragdoll agent to wrestle with another agent through curriculum learning. Teaching the agent to balance, walk, push a heavy object, use its hands, etc., out of a physics-based arena. 

---

## Main Takeaways and Challenges
* **Reward Shaping & Economy:** Balancing dense rewards (approach progress) with sparse rewards (win conditions) and existential penalties.
* **Overcoming Reward Hacking:** Diagnosing and patching unintended AI behaviors (e.g., throwing itself at the block, crawling, and reward farming).
* **Continuous Control State Spaces:** Managing a large observation space mapping 16 complex ragdoll joints, limb rotations, and velocities.

---

## Training Progression
Below is the chronological evolution of our agent's brain, showcasing how it interpreted (and frequently exploited) the reward functions it was given. We'll be updating this as our agent keeps learning new tricks.

### v0.1: (Sliding)
<table>
  <tr>
    <th width="45%">Simulation Video</th>
    <th width="55%">Architecture & Results</th>
  </tr>
  <tr>
    <td align="center">
      <br>
      <video src="https://github.com/user-attachments/assets/f0243378-e5fb-496b-a92c-a27d3fcf3496" controls muted autoplay loop style="max-width: 100%;"></video>
      <br>
    </td>
    <td>
      <b>The Goal:</b> Push the block off the edge.<br><br>
      <b>The Reward:</b> Dense progress reward + Existential penalty.<br><br>
      ✅ <b>What Worked:</b> The agent successfully moved the block.<br><br>
      ❌ <b>What Didn't:</b> Because there was no punishment for faling, the agent learned to shimmy along the floor.
    </td>
  </tr>
</table>

### v0.2: (Crawling)
<table>
  <tr>
    <th width="45%">Simulation Video</th>
    <th width="55%">Architecture & Results</th>
  </tr>
  <tr>
    <td align="center">
      <br><br> <i> <video src="https://github.com/user-attachments/assets/01a3cac8-eafd-4fbe-b378-6eccbb0aae42" controls muted autoplay loop style="max-width: 100%;"></video></i><br><br><br>
    </td>
    <td>
      <b>The Fix:</b> Added a -1.0 penalty if the chest or hips touched the floor, plus a small bonus for keeping the head high.<br><br>
      ✅ <b>What Worked:</b> The agent stopped dragging its body on the floor.<br><br>
      ❌ <b>What Didn't:</b> The agent realized its hands and feet were exempt from the penalty. It would try to catch its fall and walk in a plank position to the block.
    </td>
  </tr>
</table>

### v0.5: (A few steps...)
<table>
  <tr>
    <th width="45%">Simulation Video</th>
    <th width="55%">Architecture & Results</th>
  </tr>
  <tr>
    <td align="center">
      <br><br><i> <video src="https://github.com/user-attachments/assets/d0aa1f4a-4673-46ff-be15-08a902fe5633" controls muted autoplay loop style="max-width: 100%;"></video></i><br><br><br>
    </td>
    <td>
      <b>The Fix:</b> <ul>
        <li> Made the block heavier to stop the agent from using its feet </li>
        <li>Moved the block and rewarded the agent for getting closer (to prevent punching the block away)</li>
        <li>Added a hip threshold that punishes the agent and resets if its hips fall too low.</li>
      </ul>
      ✅ <b>What Worked:</b> The agent started taking some steps <br><br>
      ❌ <b>What Didn't:</b> The agent learned to take a few steps then throw itself onto its side, and start sliding itself to the block. since it now wanted to stick close to the block whlie moving it, it slowly slid itself into the cube to push it.
    </td>
  </tr>
</table>

### v0.6: (Bridging)
<table>
  <tr>
    <th width="45%">Simulation Video</th>
    <th width="55%">Architecture & Results</th>
  </tr>
  <tr>
    <td align="center">
      <br><br> <i><video src="https://github.com/user-attachments/assets/f49f6d9a-eb06-4f48-9c1e-0bca5b9f717f" controls muted autoplay loop style="max-width: 100%;"></video></i><br><br><br>
    </td>
    <td>
      <b>The Fix:</b> Increased the hip elevation requirement <br><br>
      ✅ <b>What Worked:</b> The agent no longer throws itself on the ground<br><br>
      ❌ <b>What Didn't:</b> To keep its center of gravity high the agent bridges off its head, still not achieving a true walk.
    </td>
  </tr>
</table>

### v0.7: Walking
<table>
  <tr>
    <th width="45%">Simulation Video</th>
    <th width="55%">Architecture & Results</th>
  </tr>
  <tr>
    <td align="center">
      <br><br> <i><video src="https://github.com/user-attachments/assets/7c2d9d8f-6991-45ca-ac6a-8a1b8e6edddc" controls muted autoplay loop style="max-width: 100%;"></video></i><br><br><br>
    </td>
    <td>
      <b>The Fix:</b> Punished the agent and reset if ANY of its limbs (except feet) touched the floor. br><br>
      ✅ <b>What Worked:</b> After 45+ million time steps, the agent successfully developed a relatively stabled walk! It is a little clumsy, but manages to walk into the block, using its legs and sometimes its arms to knock the cube out.
    </td>
  </tr>
</table>

---

## 🛠️ Next Steps (Phase 2)
With the agent now successfully walking, the next phase of the curriculum involves:
1. **Target Specificity:** Penalizing the agent for touching the block with anything other than its hands/forearms to encourage a proper push.
2. **Multi-Agent Self-Play:** Replacing the static heavy cube with a clone of the trained agent, transitioning the environment from a physics puzzle into a competitive, zero-sum environment.

--
