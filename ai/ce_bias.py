import tensorflow as tf
from typing import Optional

def get_region_proportion(x: tf.Tensor, valid_mask: tf.Tensor = None, eps: float = 1e-10) -> tf.Tensor:
    """
    Get region proportion

    Args:
        x : one-hot label map/mask
        valid_mask : indicate the considered elements
        eps : a small value to avoid division by zero
    """
    if valid_mask is not None:
        x = tf.einsum("bxyz, bxyz->bxyz", x, tf.cast(valid_mask, x.dtype))
        cardinality = tf.reduce_sum(tf.cast(valid_mask, tf.float32), axis=(1, 2, 3), keepdims=True)
    else:
        shape = tf.shape(x)
        cardinality = tf.cast(shape[1] * shape[2] * shape[3], dtype=tf.float32)

    region_proportion = (tf.reduce_sum(x, axis=(1, 2, 3)) + eps) / (cardinality + eps)

    return region_proportion

def expand_onehot_labels(labels, target_shape, ignore_index):
    """Expand onehot labels to match the size of prediction."""
    bin_labels = tf.zeros(target_shape, dtype=labels.dtype)
    valid_mask = tf.math.logical_and(labels >= 0, labels != ignore_index)
    inds = tf.where(valid_mask)

    if tf.size(inds) > 0:
        bin_labels = tf.tensor_scatter_nd_update(bin_labels, inds, tf.ones_like(inds[:, 0]))

    return bin_labels, valid_mask

class CompoundLoss(tf.keras.losses.Loss):
    BINARY_MODE = 'binary'
    MULTILABEL_MODE = 'multilabel'
    MULTICLASS_MODE = 'multiclass'
    
    def __init__(self, mode: str,
                 alpha: float = 1.,
                 factor: float = 1.,
                 step_size: int = 0,
                 max_alpha: float = 100.,
                 temp: float = 1.,
                 ignore_index: int = 255,
                 background_index: int = -1,
                 weight: Optional[tf.Tensor] = None,
                 name: str = "compound_loss",
                 **kwargs) -> None:
        assert mode in {self.BINARY_MODE, self.MULTILABEL_MODE, self.MULTICLASS_MODE}
        super().__init__(name=name, **kwargs)
        self.mode = mode
        self.alpha = alpha
        self.max_alpha = max_alpha
        self.factor = factor
        self.step_size = step_size
        self.temp = temp
        self.ignore_index = ignore_index
        self.background_index = background_index
        self.weight = weight

    def call(self, y_true, y_pred):
        return self.cross_entropy(y_pred, y_true)

    def cross_entropy(self, inputs: tf.Tensor, labels: tf.Tensor):
        # Debugging shapes

        if self.mode == self.MULTICLASS_MODE:
            loss = tf.keras.losses.sparse_categorical_crossentropy(
                labels, inputs, from_logits=True)
            if self.weight is not None:
                loss = loss * self.weight
            loss = tf.reduce_mean(loss)
        else:
            # No need to squeeze the labels tensor in this case
            if self.mode == self.BINARY_MODE:
                loss = tf.keras.losses.binary_crossentropy(labels, inputs, from_logits=True)
            else:
                loss = tf.keras.losses.binary_crossentropy(labels, inputs, from_logits=True)
            
            loss = tf.reduce_mean(loss)
        return loss

    def adjust_alpha(self, epoch: int) -> None:
        if self.step_size == 0:
            return
        if (epoch + 1) % self.step_size == 0:
            curr_alpha = self.alpha
            self.alpha = min(self.alpha * self.factor, self.max_alpha)
            print(
                "CompoundLoss : Adjust the tradeoff param alpha : {:.3g} -> {:.3g}".format(curr_alpha, self.alpha)
            )

    def get_gt_proportion(self, mode: str,
                          labels: tf.Tensor,
                          target_shape,
                          ignore_index: int = 255):
        if mode == self.MULTICLASS_MODE:
            bin_labels, valid_mask = self.expand_onehot_labels(labels, target_shape, ignore_index)
        else:
            valid_mask = (labels >= 0) & (labels != ignore_index)
            if len(labels.shape) == 3:
                labels = tf.expand_dims(labels, axis=1)
            bin_labels = labels
        gt_proportion = self.get_region_proportion(bin_labels, valid_mask)
        return gt_proportion, valid_mask

    def get_pred_proportion(self, mode: str,
                            logits: tf.Tensor,
                            temp: float = 1.0,
                            valid_mask=None):
        if mode == self.MULTICLASS_MODE:
            preds = tf.nn.softmax(temp * logits, axis=1)
        else:
            preds = tf.math.sigmoid(temp * logits)
        pred_proportion = self.get_region_proportion(preds, valid_mask)
        return pred_proportion

    def expand_onehot_labels(self, labels, target_shape, ignore_index):
        # Implementation for expanding one-hot labels
        pass
