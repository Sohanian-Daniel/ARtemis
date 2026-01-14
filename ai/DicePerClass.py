import tensorflow as tf

class DicePerClassMetric(tf.keras.metrics.Metric):
    def __init__(self, class_idx, name=None, **kwargs):
        super().__init__(name=name or f"dice_class_{class_idx}", **kwargs)
        self.class_idx = class_idx
        self.total = self.add_weight(name="total", initializer="zeros")
        self.count = self.add_weight(name="count", initializer="zeros")
    
    def update_state(self, y_true, y_pred, sample_weight=None):
        y_true_f = tf.reshape(y_true[..., self.class_idx], [-1])
        y_pred_f = tf.reshape(y_pred[..., self.class_idx], [-1])
        intersection = tf.reduce_sum(y_true_f * y_pred_f)
        dice = (2.0 * intersection + 1e-15) / (tf.reduce_sum(y_true_f) + tf.reduce_sum(y_pred_f) + 1e-15)
        self.total.assign_add(dice)
        self.count.assign_add(1.0)
    
    def result(self):
        return self.total / self.count
    
    def reset_state(self):
        self.total.assign(0.0)
        self.count.assign(0.0)
