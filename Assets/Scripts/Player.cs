using System;
using System.Security.Claims;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// The main Player behaviour.
/// </summary>
public class Player : MonoBehaviour
{
    /// <summary>
    /// Velocity multiplier applied when jumping.
    /// </summary>
    public float jumpVelocity = 100.0f;

    /// <summary>
    /// Distance to which ground should be detected.
    /// </summary>
    public float groundCheckDistance = 0.01f;

    /// <summary>
    /// Layer mask used for the ground hit detection.
    /// </summary>
    public LayerMask groundLayerMask;

    /// <summary>
    /// Layer mask used for the obstacle hit detection.
    /// </summary>
    public LayerMask obstacleLayerMask;

    /// <summary>
    /// Speed of rotation animation in degrees.
    /// </summary>
    public float rotationSpeed = 30.0f;

    /// <summary>
    /// Specifies how many points player must earn to make his character smile
    /// </summary>
    public int smileAtEvery = 10;

    /// <summary>
    /// Which axes should be rotated?
    /// </summary>
    public bool3 rotateAxis = new bool3(true, false, false);
    
    /// <summary>
    /// Direction of rotation for each axis.
    /// </summary>
    public float3 axisDirection = new float3(1.0f, 1.0f, 1.0f);

    /// <summary>
    /// Sprite used in the neutral state.
    /// </summary>
    public Sprite spriteNeutral;
    
    /// <summary>
    /// Sprite used in the happy state.
    /// </summary>
    public Sprite spriteHappy;
    
    /// <summary>
    /// Sprite used in the sad state.
    /// </summary>
    public Sprite spriteSad;
    
    /// <summary>
    /// Sprite used in the pog state.
    /// </summary>
    public Sprite spritePog;

    /// <summary>
    /// Says ho many jumps are necessary to perform special jump
    /// </summary>
    public int pogJump = 3;
    
    /// <summary>
    /// Our RigidBody used for physics simulation.
    /// </summary>
    private Rigidbody2D mRB;
    
    /// <summary>
    /// Our BoxCollider used for collision detection.
    /// </summary>
    private BoxCollider2D mBC;
    
    /// <summary>
    /// Sprite renderer of the child sprite GameObject.
    /// </summary>
    private SpriteRenderer mSpriteRenderer;

    /// <summary>
    /// Transform of the child sprite GameObject.
    /// </summary>
    private Transform mSpriteTransform;

    /// <summary>
    /// Target angle of rotation in degrees.
    /// </summary>
    private Quaternion mTargetRotation;

    /// <summary>
    /// Remember when we switch gravity to prevent "hovering".
    /// </summary>
    private bool mSwitchedGravity = false;
    
    /// <summary>
    /// Current state of gravity - 1.0 for down, -1.0f for up.
    /// </summary>
    private float mCurrentGravity = 1.0f;

    ///<summary>
    ///Counter of jumps
    ///</summary>
    private int jumpCnt = 0;

    ///<summary>
    ///Special flag that indicates scpecial jump with rotation
    ///</summary>
    private bool  pogJumping = false;
    
    /// <summary>
    /// Called before the first frame update.
    /// </summary>
    void Start()
    {
        mRB = GetComponent<Rigidbody2D>();
        mBC = GetComponent<BoxCollider2D>();
        mSpriteRenderer = gameObject.transform.GetChild(0).GetComponent<SpriteRenderer>();
        mSpriteTransform = gameObject.transform.GetChild(0).GetComponent<Transform>();
        mTargetRotation = mSpriteTransform.rotation;
    }

    /// <summary>
    /// Update called once per frame.
    /// </summary>
    void Update()
    {
        // Process player input.
        var verticalMovement = Input.GetAxisRaw("Vertical");
        var horizontalMovement = Input.GetAxisRaw("Horizontal");
        var jumpMovement = Input.GetButtonDown("Jump");
        var onGround = IsOnGround();
        
        // Reset gravity switch if we are on the ground.
        mSwitchedGravity &= !onGround;

        // Impart the initial impulse if we are jumping.
        if (jumpMovement && onGround)
        { 
            setSprite(spriteNeutral);
            pogJumping = false;
            mRB.velocity = -Physics2D.gravity * jumpVelocity;

            jumpCnt++;
            if(jumpCnt == pogJump) {
                jumpCnt = 0;
                setSprite(spritePog);
                pogJumping = true;
            }
        }
        
        // Switch gravity with vertical movement.
        if (verticalMovement != 0.0 && !mSwitchedGravity)
        {
            mCurrentGravity = verticalMovement > 0.0f ? 1.0f : -1.0f;
            Physics2D.gravity = mCurrentGravity * new Vector2(
                Math.Abs(Physics2D.gravity.x),
                Math.Abs(Physics2D.gravity.y)
            );
            mTargetRotation = Quaternion.Euler(new float3(
                rotateAxis.x && mCurrentGravity > 0.0f ? 180.0f : 0.0f, 
                rotateAxis.y && mCurrentGravity > 0.0f ? 180.0f : 0.0f, 
                rotateAxis.z && mCurrentGravity > 0.0f ? 180.0f : 0.0f
            ) * axisDirection);
            mSwitchedGravity = true;
        }

           // transform.Rotate(0f,0f,rotationSpeed * Time.fixedDeltaTime);
    }

    /// <summary>
    /// Check if we are on the ground.
    /// </summary>
    /// <returns>Returns true if we are on solid ground.</returns>
    bool IsOnGround()
    {
        // Cast our current BoxCollider in the current gravity direction.
        var hitInfo = Physics2D.BoxCast(
            mBC.bounds.center, mBC.bounds.size, 
            0.0f, Physics2D.gravity.normalized, groundCheckDistance, 
            groundLayerMask
        );

        return hitInfo.collider != null;
    }

    public void setSprite(Sprite target) {
        mSpriteRenderer.sprite = target;
    }

    /// <summary>
    /// Update called for every update interval.
    /// </summary>
    private void FixedUpdate()
    {
        var onGround = IsOnGround();
        
        if (!onGround && pogJumping)
        {   
            //Every pog jump player rotates
            transform.Rotate(0f, 0f, rotationSpeed * Time.fixedDeltaTime);
        }
        else
        { 
            //Normally he doesnt rotate
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }
    
    /// <summary>
    /// Event triggered when we collide with something.
    /// </summary>
    /// <param name="other">The thing we collided with.</param>
    private void OnCollisionEnter2D(Collision2D other)
    {
        // Check the collided object against the layer mask.
        var hitObstacle = mBC.IsTouchingLayers(obstacleLayerMask);
        
        if (hitObstacle)
        { 
            pogJumping = true;
            // If we collide with any obstacle -> end the game.
            // Update the sprite.
            setSprite(spriteSad);
            // Move to the uncollidable layer.
            gameObject.layer = LayerMask.NameToLayer("Uncollidable");
            // Fling the obstacle out of the screen.
            var otherRB = other.gameObject.GetComponent<Rigidbody2D>();
            other.gameObject.layer = LayerMask.NameToLayer("Uncollidable");
            otherRB.velocity = -Physics.gravity;
            // Loose the game.
            GameManager.Instance.LooseGame();
        }
    }
}
