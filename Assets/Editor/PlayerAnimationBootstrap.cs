using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// Ensures placeholder player animations and animator controller exist.
public static class PlayerAnimationBootstrap
{
    private const string AnimFolder = "Assets/Animations/Player";
    private const string ControllerPath = AnimFolder + "/Player.controller";
    private const string IdleClipPath = AnimFolder + "/Idle.anim";
    private const string MoveClipPath = AnimFolder + "/Move.anim";
    private const string HitClipPath = AnimFolder + "/Hit.anim";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
    private const string VisualsName = "Visuals";
    private const string AttackPointName = "AttackPoint";

    [InitializeOnLoadMethod]
    private static void EnsurePlayerAnimator()
    {
        if (!Directory.Exists(AnimFolder))
            Directory.CreateDirectory(AnimFolder);

        var idle = EnsureClip("Idle", IdleClipPath);
        var move = EnsureClip("Move", MoveClipPath);
        var hit = EnsureClip("Hit", HitClipPath);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;
        var idleState = EnsureState(sm, "Idle", idle, new Vector3(0f, 0f));
        var moveState = EnsureState(sm, "Move", move, new Vector3(250f, 0f));
        var hitState = EnsureState(sm, "Hit", hit, new Vector3(125f, -150f));
        sm.defaultState = idleState;

        EnsureTransition(idleState, moveState, "IsMoving", true);
        EnsureTransition(moveState, idleState, "IsMoving", false);

        EnsureAnyStateToHit(sm, hitState);
        EnsureExitToIdle(hitState, idleState);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        AssignControllerToPlayerPrefab(controller);
    }

    private static AnimationClip EnsureClip(string name, string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
        }
        return clip;
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (controller.parameters.Any(p => p.name == name))
            return;
        controller.AddParameter(name, type);
    }

    private static AnimatorState EnsureState(AnimatorStateMachine sm, string name, Motion motion, Vector3 position)
    {
        var state = sm.states.FirstOrDefault(s => s.state.name == name).state;
        if (state == null)
        {
            state = sm.AddState(name, position);
        }
        state.motion = motion;
        return state;
    }

    private static void EnsureTransition(AnimatorState from, AnimatorState to, string boolParam, bool value)
    {
        var existing = from.transitions.FirstOrDefault(t => t.destinationState == to);
        if (existing == null)
        {
            existing = from.AddTransition(to);
            existing.hasExitTime = false;
            existing.duration = 0.05f;
        }

        if (value)
        {
            if (!existing.conditions.Any(c => c.mode == AnimatorConditionMode.If && c.parameter == boolParam))
                existing.AddCondition(AnimatorConditionMode.If, 0f, boolParam);
        }
        else
        {
            if (!existing.conditions.Any(c => c.mode == AnimatorConditionMode.IfNot && c.parameter == boolParam))
                existing.AddCondition(AnimatorConditionMode.IfNot, 0f, boolParam);
        }
    }

    private static void EnsureAnyStateToHit(AnimatorStateMachine sm, AnimatorState hitState)
    {
        var existing = sm.anyStateTransitions.FirstOrDefault(t => t.destinationState == hitState);
        if (existing == null)
        {
            existing = sm.AddAnyStateTransition(hitState);
            existing.hasExitTime = false;
            existing.duration = 0.05f;
        }

        if (!existing.conditions.Any(c => c.parameter == "Attack"))
            existing.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
    }

    private static void EnsureExitToIdle(AnimatorState hitState, AnimatorState idleState)
    {
        var existing = hitState.transitions.FirstOrDefault(t => t.destinationState == idleState);
        if (existing == null)
        {
            existing = hitState.AddTransition(idleState);
            existing.hasExitTime = true;
            existing.exitTime = 1f;
            existing.duration = 0.05f;
        }
    }

    private static void AssignControllerToPlayerPrefab(AnimatorController controller)
    {
        if (!File.Exists(PlayerPrefabPath))
            return;

        var prefab = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        var visuals = EnsureVisuals(prefab.transform);
        EnsureAttackPoint(visuals);

        var animator = visuals.GetComponent<Animator>();
        if (animator == null)
            animator = visuals.gameObject.AddComponent<Animator>();

        if (animator.runtimeAnimatorController != controller)
            animator.runtimeAnimatorController = controller;

        var player = prefab.GetComponent<PlayerController>();
        if (player != null)
        {
            var so = new SerializedObject(player);
            so.FindProperty("animator").objectReferenceValue = animator;
            so.FindProperty("spriteRenderer").objectReferenceValue = visuals.GetComponent<SpriteRenderer>();
            so.FindProperty("attackPoint").objectReferenceValue = visuals.Find(AttackPointName);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(prefab, PlayerPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);
    }

    private static Transform EnsureVisuals(Transform root)
    {
        var visuals = root.Find(VisualsName);
        if (visuals == null)
        {
            var go = new GameObject(VisualsName);
            visuals = go.transform;
            visuals.SetParent(root);
            visuals.localPosition = Vector3.zero;
            visuals.localRotation = Quaternion.identity;
            visuals.localScale = Vector3.one;
        }

        var rootSprite = root.GetComponent<SpriteRenderer>();
        if (rootSprite != null && visuals.GetComponent<SpriteRenderer>() == null)
        {
            var newSr = visuals.gameObject.AddComponent<SpriteRenderer>();
            EditorUtility.CopySerialized(rootSprite, newSr);
            Object.DestroyImmediate(rootSprite, true);
        }

        var rootAnimator = root.GetComponent<Animator>();
        if (rootAnimator != null && visuals.GetComponent<Animator>() == null)
        {
            var newAnim = visuals.gameObject.AddComponent<Animator>();
            EditorUtility.CopySerialized(rootAnimator, newAnim);
            Object.DestroyImmediate(rootAnimator, true);
        }

        return visuals;
    }

    private static void EnsureAttackPoint(Transform visuals)
    {
        if (visuals == null) return;
        var ap = visuals.Find(AttackPointName);
        if (ap == null)
        {
            var go = new GameObject(AttackPointName);
            ap = go.transform;
            ap.SetParent(visuals);
            ap.localPosition = new Vector3(0.7f, 0f, 0f);
            ap.localRotation = Quaternion.identity;
            ap.localScale = Vector3.one;
        }
    }
}
