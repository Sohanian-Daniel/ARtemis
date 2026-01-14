import tensorflow as tf

SMOOTH = 1e-15
NUM_CLASSES = 6  # background + 5 masks
# Updated weights based on class rarity: 0=BG, 1=Plastic, 2=Paper, 3=Bio, 4=Metal, 5=Other
CLASS_WEIGHTS = tf.constant([6.5, 7.0, 15.0, 40.0, 10.0, 7.0], dtype=tf.float32)
NORMALIZED_CLASS_WEIGHTS = CLASS_WEIGHTS / tf.reduce_sum(CLASS_WEIGHTS)

# ---------------- PIXEL PRECISION ----------------
def pixel_precision(y_true, y_pred):
    y_true_labels = tf.argmax(y_true, axis=-1)
    y_pred_labels = tf.argmax(y_pred, axis=-1)
    
    y_true_labels = tf.reshape(y_true_labels, [-1])
    y_pred_labels = tf.reshape(y_pred_labels, [-1])
    
    correct = tf.reduce_sum(tf.cast(tf.equal(y_true_labels, y_pred_labels), tf.float32))
    total = tf.cast(tf.size(y_pred_labels), tf.float32)
    
    return correct / (total + 1e-7)

# ---------------- PER CLASS PRECISION ----------------
def per_class_precision(y_true, y_pred):
    y_true_labels = tf.argmax(y_true, axis=-1)
    y_pred_labels = tf.argmax(y_pred, axis=-1)

    num_classes = tf.shape(y_true)[-1]

    y_true_one_hot = tf.one_hot(y_true_labels, num_classes)
    y_pred_one_hot = tf.one_hot(y_pred_labels, num_classes)

    tp = tf.reduce_sum(y_true_one_hot * y_pred_one_hot, axis=[0,1,2])
    predicted = tf.reduce_sum(y_pred_one_hot, axis=[0,1,2])

    per_class = tp / (predicted + 1e-7)

    ## print per-mask precision for foreground classes
    #tf.print("Per-class precision:", per_class[1:])

    # return mean of foreground classes as scalar for Keras
    return tf.reduce_mean(per_class[1:])

# ---------------- MULTI-CLASS DICE COEFFICIENT ----------------
def dice_coef_multi(y_true, y_pred, smooth=SMOOTH):
    y_true_f = tf.reshape(y_true, [-1, NUM_CLASSES])
    y_pred_f = tf.reshape(y_pred, [-1, NUM_CLASSES])

    intersection = tf.reduce_sum(y_true_f * y_pred_f, axis=0)
    denominator = tf.reduce_sum(y_true_f + y_pred_f, axis=0)

    dice_per_class = (2.0 * intersection + smooth) / (denominator + smooth)

    # print per-mask dice for foreground classes
    #tf.print("Per-class dice:", dice_per_class[1:])

    # return mean of foreground classes as scalar for Keras
    return tf.reduce_mean(dice_per_class[1:])

# ---------------- MULTI-CLASS DICE LOSS ----------------
def dice_loss_multi(y_true, y_pred):
    y_true_f = tf.reshape(y_true, [-1, NUM_CLASSES])
    y_pred_f = tf.reshape(y_pred, [-1, NUM_CLASSES])

    intersection = tf.reduce_sum(y_true_f * y_pred_f, axis=0)
    denominator = tf.reduce_sum(y_true_f + y_pred_f, axis=0)

    dice_per_class = (2.0 * intersection + SMOOTH) / (denominator + SMOOTH)

    # weighted dice for training loss
    weighted_dice = dice_per_class * NORMALIZED_CLASS_WEIGHTS
    return 1.0 - tf.reduce_sum(weighted_dice)

# ---------------- COMBINED LOSS ----------------
def combined_loss(y_true, y_pred):
    # Focal Loss Parameters
    gamma = 2.0
    epsilon = 1e-7
    
    # Clip predictions to prevent log(0)
    y_pred = tf.clip_by_value(y_pred, epsilon, 1.0 - epsilon)
    
    # Calculate Weighted Focal Loss
    # Formula: -alpha * (1 - p_t)^gamma * log(p_t)
    cross_entropy = -y_true * tf.math.log(y_pred)
    focal_weight = tf.math.pow(1.0 - y_pred, gamma)
    
    # CLASS_WEIGHTS acts as alpha
    focal_loss = CLASS_WEIGHTS * focal_weight * cross_entropy
    focal_loss = tf.reduce_mean(tf.reduce_sum(focal_loss, axis=-1))

    d_loss = dice_loss_multi(y_true, y_pred)
    return focal_loss + d_loss
