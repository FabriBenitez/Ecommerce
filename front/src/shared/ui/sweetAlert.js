import Swal from "sweetalert2";

const baseConfig = {
  confirmButtonColor: "#111111",
  cancelButtonColor: "#6b7280",
  reverseButtons: true,
};

export function notifySuccess(title, text = "") {
  return Swal.fire({
    ...baseConfig,
    icon: "success",
    title,
    text,
  });
}

export function notifyError(title, text = "") {
  return Swal.fire({
    ...baseConfig,
    icon: "error",
    title,
    text,
  });
}

export function notifyWarning(title, text = "") {
  return Swal.fire({
    ...baseConfig,
    icon: "warning",
    title,
    text,
  });
}

export async function confirmAction({
  title,
  text = "",
  confirmText = "Confirmar",
  cancelText = "Cancelar",
  icon = "question",
}) {
  const result = await Swal.fire({
    ...baseConfig,
    icon,
    title,
    text,
    showCancelButton: true,
    confirmButtonText: confirmText,
    cancelButtonText: cancelText,
  });

  return result.isConfirmed;
}
